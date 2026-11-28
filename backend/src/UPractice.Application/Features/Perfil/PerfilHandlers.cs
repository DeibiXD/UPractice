using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Auth;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Perfil;

/// <summary>Lectura del perfil propio. Sin fila en PerfilesEstudiante, los datos académicos vienen en null y el estado es Pendiente.</summary>
internal static class ConsultasPerfil
{
    public static Guid EstudianteActual(ICurrentUser actual) =>
        actual is { Rol: Rol.Estudiante, UsuarioId: { } id }
            ? id
            : throw new NoAutenticadoException("La sesión ya no es válida. Inicie sesión de nuevo.");

    public static async Task<PerfilEstudianteDto> ObtenerAsync(IAppDbContext db, Guid usuarioId, CancellationToken ct)
    {
        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Rol == Rol.Estudiante)
            .Select(u => new
            {
                u.Id, u.Nombre1, u.Nombre2, u.Apellido1, u.Apellido2, u.Correo,
                UniversidadId = u.UniversidadId!.Value, Universidad = u.Universidad!.Nombre,
            })
            .SingleOrDefaultAsync(ct) ?? throw new NoEncontradoException("El estudiante no existe.");

        var perfil = await db.PerfilesEstudiante.AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new
            {
                p.NumeroCuenta, p.IndiceAcademico, p.MostrarIndice, p.EstadoVerificacion,
                p.ObservacionVerificacion, p.FechaVerificacion,
                Carrera = new CarreraDto(p.CarreraId, p.Carrera.Nombre, p.Carrera.Facultad.Nombre, p.Carrera.Facultad.UniversidadId),
            })
            .SingleOrDefaultAsync(ct);

        var habilidades = await db.EstudianteHabilidades.AsNoTracking()
            .Where(h => h.EstudianteId == usuarioId)
            .OrderBy(h => h.Habilidad.Nombre)
            .Select(h => new HabilidadDto(h.HabilidadId, h.Habilidad.Nombre))
            .ToListAsync(ct);

        return new PerfilEstudianteDto(
            usuario.Id,
            Usuario.ArmarNombreCompleto(usuario.Nombre1, usuario.Nombre2, usuario.Apellido1, usuario.Apellido2),
            usuario.Correo,
            new Referencia(usuario.UniversidadId, usuario.Universidad),
            perfil?.NumeroCuenta,
            perfil?.Carrera,
            perfil?.IndiceAcademico,
            habilidades,
            perfil?.EstadoVerificacion ?? EstadoVerificacion.Pendiente,
            perfil?.ObservacionVerificacion,
            perfil?.FechaVerificacion,
            new PrivacidadDto(perfil?.MostrarIndice ?? false));
    }
}

/// <summary>GET /estudiantes/me/perfil (HU-02).</summary>
public sealed class ObtenerMiPerfilHandler(IAppDbContext db, ICurrentUser actual)
{
    public Task<PerfilEstudianteDto> EjecutarAsync(CancellationToken ct) =>
        ConsultasPerfil.ObtenerAsync(db, ConsultasPerfil.EstudianteActual(actual), ct);
}

/// <summary>
/// PUT /estudiantes/me/perfil/academico (HU-02). Crea el perfil o lo edita mientras esté Pendiente o Rechazado;
/// tras un rechazo vuelve a Pendiente. Verificado → 422 PERFIL_VERIFICADO_INMUTABLE.
/// </summary>
public sealed class ActualizarPerfilAcademicoHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<PerfilEstudianteDto> EjecutarAsync(ActualizarPerfilAcademicoRequest request, CancellationToken ct)
    {
        var usuarioId = ConsultasPerfil.EstudianteActual(actual);
        var universidadId = await db.Usuarios.Where(u => u.Id == usuarioId)
            .Select(u => u.UniversidadId).SingleOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("El estudiante no existe.");

        var perfil = await db.PerfilesEstudiante.SingleOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);
        if (perfil?.EstadoVerificacion == EstadoVerificacion.Verificado)
            throw new ReglaNegocioException(CodigosError.PerfilVerificadoInmutable, MensajesPerfil.PerfilVerificado);

        var numeroCuenta = request.NumeroCuenta!.Trim();
        var errores = new List<ValidationFailure>();

        // La carrera debe ser del catálogo activo de SU universidad.
        var carreraValida = await db.Carreras.AnyAsync(c => c.Id == request.CarreraId && c.Activa
            && c.Facultad.UniversidadId == universidadId && c.Facultad.Universidad.Activa, ct);
        if (!carreraValida) errores.Add(new ValidationFailure("carreraId", MensajesPerfil.Carrera));

        // Número de cuenta único por universidad (S3.4: se valida aquí porque UniversidadId vive en Usuarios).
        var cuentaEnUso = await db.PerfilesEstudiante.IgnoreQueryFilters().AnyAsync(p =>
            p.NumeroCuenta == numeroCuenta && p.UsuarioId != usuarioId && p.Usuario.UniversidadId == universidadId, ct);
        if (cuentaEnUso) errores.Add(new ValidationFailure("numeroCuenta", MensajesPerfil.NumeroCuentaEnUso));

        if (errores.Count > 0) throw new ValidationException(errores);

        if (perfil is null)
        {
            perfil = new PerfilEstudiante { UsuarioId = usuarioId };
            db.PerfilesEstudiante.Add(perfil);
        }
        perfil.NumeroCuenta = numeroCuenta;
        perfil.CarreraId = request.CarreraId;
        perfil.IndiceAcademico = request.IndiceAcademico!.Value;
        // Nueva solicitud de verificación: se limpia la resolución anterior (rechazo).
        perfil.EstadoVerificacion = EstadoVerificacion.Pendiente;
        perfil.ObservacionVerificacion = null;
        perfil.VerificadoPorId = null;
        perfil.FechaVerificacion = null;

        await db.SaveChangesAsync(ct);
        return await ConsultasPerfil.ObtenerAsync(db, usuarioId, ct);
    }
}

/// <summary>PUT /estudiantes/me/habilidades (HU-02). Reemplaza la lista; se permite aunque el perfil esté verificado.</summary>
public sealed class ActualizarHabilidadesHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<PerfilEstudianteDto> EjecutarAsync(ActualizarHabilidadesRequest request, CancellationToken ct)
    {
        var usuarioId = ConsultasPerfil.EstudianteActual(actual);
        var ids = request.HabilidadesIds!;

        var existentes = await db.Habilidades.CountAsync(h => ids.Contains(h.Id), ct);
        if (existentes != ids.Count)
            throw new ValidationException([new ValidationFailure("habilidadesIds", MensajesPerfil.HabilidadesInvalidas)]);

        var actuales = await db.EstudianteHabilidades.Where(h => h.EstudianteId == usuarioId).ToListAsync(ct);
        db.EstudianteHabilidades.RemoveRange(actuales.Where(h => !ids.Contains(h.HabilidadId)));
        foreach (var id in ids.Where(id => actuales.All(h => h.HabilidadId != id)))
            db.EstudianteHabilidades.Add(new EstudianteHabilidad { EstudianteId = usuarioId, HabilidadId = id });

        await db.SaveChangesAsync(ct);
        return await ConsultasPerfil.ObtenerAsync(db, usuarioId, ct);
    }
}

/// <summary>PUT /estudiantes/me/privacidad (HU-02): consentimiento para que las empresas vean el índice.</summary>
public sealed class ActualizarPrivacidadHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task EjecutarAsync(PrivacidadDto request, CancellationToken ct)
    {
        var usuarioId = ConsultasPerfil.EstudianteActual(actual);
        var perfil = await db.PerfilesEstudiante.SingleOrDefaultAsync(p => p.UsuarioId == usuarioId, ct)
            ?? throw new ReglaNegocioException(CodigosError.PerfilIncompleto, MensajesPerfil.PerfilIncompleto);
        perfil.MostrarIndice = request.MostrarIndice;
        await db.SaveChangesAsync(ct);
    }
}
