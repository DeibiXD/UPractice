using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Coordinacion.Verificacion;

// ===== Contratos (S5.6) =====

public sealed record ListarEstudiantesVerificacionRequest(
    EstadoVerificacion? EstadoVerificacion = null, int Pagina = 1, int TamanoPagina = 10);

public sealed record EstudianteVerificacionDto(
    Guid UsuarioId,
    string NombreCompleto,
    string Correo,
    string NumeroCuenta,
    string Carrera,
    decimal IndiceAcademico,
    EstadoVerificacion EstadoVerificacion);

/// <summary><c>Aprobar</c> es nullable a propósito: un cuerpo vacío da 400 en lugar de rechazar por omisión.</summary>
public sealed record ResolverVerificacionRequest(bool? Aprobar, string? Observacion);

public static class MensajesVerificacion
{
    public const string Estado = "Seleccione un estado de verificación válido.";
    public const string Pagina = "La página debe ser mayor o igual a 1.";
    public const string TamanoPagina = "El tamaño de página debe estar entre 1 y 50.";
    public const string Aprobar = "Indique si verifica o rechaza el perfil.";
    public const string Observacion = "Indique la observación (10 a 500 caracteres).";
    public const string SoloPendientes = "Solo se pueden resolver perfiles en estado Pendiente.";
}

// ===== Validadores =====

public sealed class ListarEstudiantesVerificacionValidator : AbstractValidator<ListarEstudiantesVerificacionRequest>
{
    public ListarEstudiantesVerificacionValidator()
    {
        RuleFor(x => x.EstadoVerificacion).IsInEnum().WithMessage(MensajesVerificacion.Estado);
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1).WithMessage(MensajesVerificacion.Pagina);
        RuleFor(x => x.TamanoPagina).InclusiveBetween(1, 50).WithMessage(MensajesVerificacion.TamanoPagina);
    }
}

public sealed class ResolverVerificacionValidator : AbstractValidator<ResolverVerificacionRequest>
{
    public ResolverVerificacionValidator()
    {
        RuleFor(x => x.Aprobar).NotNull().WithMessage(MensajesVerificacion.Aprobar);
        // Obligatoria (10–500) al rechazar; opcional (máx. 500) al verificar.
        RuleFor(x => x.Observacion)
            .Must(o => o is { } t && t.Trim().Length is >= 10 and <= 500).WithMessage(MensajesVerificacion.Observacion)
            .When(x => x.Aprobar == false);
        RuleFor(x => x.Observacion)
            .Must(o => o is null || o.Trim().Length <= 500).WithMessage(MensajesVerificacion.Observacion)
            .When(x => x.Aprobar == true);
    }
}

// ===== Handlers =====

/// <summary>
/// GET /coordinacion/estudiantes?estadoVerificacion=&amp;pagina=&amp;tamanoPagina= (HU-C02).
/// El filtro global limita PerfilesEstudiante a la universidad del coordinador (S3.2).
/// </summary>
public sealed class ListarEstudiantesVerificacionHandler(IAppDbContext db)
{
    public async Task<PaginaResultado<EstudianteVerificacionDto>> EjecutarAsync(
        ListarEstudiantesVerificacionRequest request, CancellationToken ct)
    {
        var consulta = db.PerfilesEstudiante.AsNoTracking()
            .Where(p => request.EstadoVerificacion == null || p.EstadoVerificacion == request.EstadoVerificacion);

        var total = await consulta.CountAsync(ct);
        var filas = await consulta
            .OrderBy(p => p.Usuario.Apellido1).ThenBy(p => p.Usuario.Nombre1).ThenBy(p => p.UsuarioId)
            .Skip((request.Pagina - 1) * request.TamanoPagina).Take(request.TamanoPagina)
            .Select(p => new
            {
                p.UsuarioId, p.Usuario.Nombre1, p.Usuario.Nombre2, p.Usuario.Apellido1, p.Usuario.Apellido2,
                p.Usuario.Correo, p.NumeroCuenta, Carrera = p.Carrera.Nombre, p.IndiceAcademico, p.EstadoVerificacion,
            })
            .ToListAsync(ct);

        var items = filas.Select(f => new EstudianteVerificacionDto(
            f.UsuarioId, Usuario.ArmarNombreCompleto(f.Nombre1, f.Nombre2, f.Apellido1, f.Apellido2),
            f.Correo, f.NumeroCuenta, f.Carrera, f.IndiceAcademico, f.EstadoVerificacion)).ToList();

        var paginas = (int)Math.Ceiling(total / (double)request.TamanoPagina);
        return new PaginaResultado<EstudianteVerificacionDto>(items, request.Pagina, request.TamanoPagina, total, paginas);
    }
}

/// <summary>
/// PATCH /coordinacion/estudiantes/{usuarioId}/verificacion (HU-C02). Solo desde Pendiente. Registra quién y cuándo;
/// al verificar, los datos académicos quedan bloqueados (HU-02 responde 422 PERFIL_VERIFICADO_INMUTABLE).
/// Un estudiante de otra universidad no existe para el coordinador → 404.
/// </summary>
public sealed class ResolverVerificacionHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task EjecutarAsync(Guid usuarioId, ResolverVerificacionRequest request, CancellationToken ct)
    {
        if (actual is not { Rol: Rol.Coordinador, UsuarioId: { } coordinadorId })
            throw new NoAutenticadoException("La sesión ya no es válida. Inicie sesión de nuevo.");

        var perfil = await db.PerfilesEstudiante.SingleOrDefaultAsync(p => p.UsuarioId == usuarioId, ct)
            ?? throw new NoEncontradoException("El estudiante no existe.");
        if (perfil.EstadoVerificacion != EstadoVerificacion.Pendiente)
            throw new ReglaNegocioException(CodigosError.TransicionInvalida, MensajesVerificacion.SoloPendientes);

        var observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim();
        perfil.EstadoVerificacion = request.Aprobar == true ? EstadoVerificacion.Verificado : EstadoVerificacion.Rechazado;
        perfil.ObservacionVerificacion = observacion;
        perfil.VerificadoPorId = coordinadorId;
        perfil.FechaVerificacion = fecha.UtcAhora();
        await db.SaveChangesAsync(ct);
    }
}
