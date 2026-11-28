using Microsoft.EntityFrameworkCore;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>
/// Estudiantes propios de las pruebas de HU-02, HU-03 y HU-C02 (con o sin perfil) que se borran al terminar,
/// para no alterar el seed de demostración que cuentan las pruebas de aislamiento.
/// </summary>
public sealed class EscenarioPerfil(ApiFixture api) : IAsyncDisposable
{
    private readonly List<Guid> estudiantes = [];

    public sealed record Estudiante(Guid Id, string Correo);

    /// <summary>Estudiante de la UNAH (o UNITEC). Sin <paramref name="estado"/> no tiene fila de perfil.</summary>
    public async Task<Estudiante> CrearEstudianteAsync(EstadoVerificacion? estado = null, bool unitec = false,
        string? numeroCuenta = null, string? observacion = null)
    {
        var referencia = await Datos.UsuarioAsync(api, Datos.EstudianteUnah);
        await using var db = api.CrearContextoSinUsuario();
        var universidad = await db.Universidades.AsNoTracking().SingleAsync(u => u.DominioCorreo == (unitec ? "unitec.edu" : "unah.hn"));

        var usuario = new Usuario
        {
            Correo = $"perfil.{Guid.NewGuid():N}@{universidad.DominioCorreo}",
            PasswordHash = referencia.PasswordHash,   // misma contraseña de demostración
            Nombre1 = "Prueba",
            Apellido1 = "Perfil",
            Rol = Rol.Estudiante,
            UniversidadId = universidad.Id,
        };
        if (estado is { } e)
        {
            var carreraId = await CarreraAsync(universidad.Id);
            usuario.Perfil = new PerfilEstudiante
            {
                NumeroCuenta = numeroCuenta ?? $"77{Random.Shared.NextInt64(10_000_000, 99_999_999)}",
                CarreraId = carreraId,
                IndiceAcademico = 80m,
                EstadoVerificacion = e,
                ObservacionVerificacion = observacion,
                FechaVerificacion = e == EstadoVerificacion.Pendiente ? null : DateTime.UtcNow,
            };
        }
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        estudiantes.Add(usuario.Id);
        return new Estudiante(usuario.Id, usuario.Correo);
    }

    /// <summary>Una carrera activa de la universidad (Ingeniería en Sistemas en la UNAH).</summary>
    public async Task<Guid> CarreraAsync(Guid universidadId)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Carreras.AsNoTracking()
            .Where(c => c.Activa && c.Facultad.UniversidadId == universidadId)
            .OrderBy(c => c.Nombre).Select(c => c.Id).FirstAsync();
    }

    public async Task<Guid> UniversidadAsync(string dominio)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Universidades.AsNoTracking().Where(u => u.DominioCorreo == dominio).Select(u => u.Id).SingleAsync();
    }

    public async Task<PerfilEstudiante?> PerfilAsync(Guid usuarioId)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.PerfilesEstudiante.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == usuarioId);
    }

    public async Task<List<Curriculum>> CurriculumsAsync(Guid usuarioId)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Curriculums.IgnoreQueryFilters().AsNoTracking().Where(c => c.EstudianteId == usuarioId).ToListAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = api.CrearContextoSinUsuario();
        var postulaciones = await db.Postulaciones.IgnoreQueryFilters().Where(p => estudiantes.Contains(p.EstudianteId)).Select(p => p.Id).ToListAsync();
        await db.HistorialPostulacion.IgnoreQueryFilters().Where(h => postulaciones.Contains(h.PostulacionId)).ExecuteDeleteAsync();
        await db.Postulaciones.IgnoreQueryFilters().Where(p => postulaciones.Contains(p.Id)).ExecuteDeleteAsync();
        await db.EstudianteHabilidades.IgnoreQueryFilters().Where(h => estudiantes.Contains(h.EstudianteId)).ExecuteDeleteAsync();
        await db.Curriculums.IgnoreQueryFilters().Where(c => estudiantes.Contains(c.EstudianteId)).ExecuteDeleteAsync();
        await db.PerfilesEstudiante.IgnoreQueryFilters().Where(p => estudiantes.Contains(p.UsuarioId)).ExecuteDeleteAsync();
        await db.Usuarios.Where(u => estudiantes.Contains(u.Id)).ExecuteDeleteAsync();
    }
}
