using Microsoft.EntityFrameworkCore;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>
/// Crea los estudiantes y vacantes propios de una prueba y los borra al terminar (await using). Así no se altera el seed
/// de demostración, cuyas filas cuentan las pruebas de aislamiento (p. ej. "digital tiene exactamente una postulación").
/// </summary>
public sealed class EscenarioPostulaciones(ApiFixture api) : IAsyncDisposable
{
    private readonly List<Guid> estudiantes = [];
    private readonly List<Guid> vacantes = [];

    public sealed record EstudianteCreado(Guid Id, string Correo, Guid CurriculumId);

    /// <summary>Estudiante de la UNAH, por defecto de Ingeniería en Sistemas, verificado y con un CV.</summary>
    public async Task<EstudianteCreado> CrearEstudianteAsync(
        decimal indice = 90m, bool verificado = true, bool mostrarIndice = true)
    {
        var ana = await Datos.UsuarioAsync(api, Datos.EstudianteUnah);
        var coordinador = await Datos.UsuarioAsync(api, Datos.CoordinadorUnah);

        await using var db = api.CrearContextoSinUsuario();
        var carreraId = (await db.PerfilesEstudiante.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.UsuarioId == ana.Id)).CarreraId;

        var usuario = new Usuario
        {
            Correo = $"prueba.{Guid.NewGuid():N}@unah.hn",
            PasswordHash = ana.PasswordHash,   // misma contraseña de demostración
            Nombre1 = "Prueba",
            Apellido1 = "Postulante",
            Rol = Rol.Estudiante,
            UniversidadId = ana.UniversidadId,
        };
        usuario.Perfil = new PerfilEstudiante
        {
            NumeroCuenta = $"2020{Random.Shared.NextInt64(10_000_000, 99_999_999)}",
            CarreraId = carreraId,
            IndiceAcademico = indice,
            MostrarIndice = mostrarIndice,
            EstadoVerificacion = verificado ? EstadoVerificacion.Verificado : EstadoVerificacion.Pendiente,
            VerificadoPorId = verificado ? coordinador.Id : null,
            FechaVerificacion = verificado ? DateTime.UtcNow : null,
        };
        var cv = new Curriculum { EstudianteId = usuario.Id, NombreVersion = "CV de prueba", EsPrincipal = true };
        cv.ArchivoKey = $"curriculums/{usuario.Id}/{cv.Id}.pdf";

        db.Usuarios.Add(usuario);
        db.Curriculums.Add(cv);
        await db.SaveChangesAsync();

        estudiantes.Add(usuario.Id);
        return new EstudianteCreado(usuario.Id, usuario.Correo, cv.Id);
    }

    /// <summary>Id de una carrera de la UNAH por nombre (para probar vacantes que no aceptan Sistemas).</summary>
    public async Task<Guid> CarreraIdAsync(string nombre)
    {
        await using var db = api.CrearContextoSinUsuario();
        return (await db.Carreras.AsNoTracking().FirstAsync(c => c.Nombre == nombre)).Id;
    }

    /// <summary>Vacante de Soluciones Digitales HN (empresa A). Por defecto publicada, vigente y para Ingeniería en Sistemas de la UNAH.</summary>
    public async Task<Guid> CrearVacanteAsync(
        int cupos = 2, decimal? indiceMinimo = null, Guid? carreraId = null, EstadoVacante estado = EstadoVacante.Publicada, int diasParaCerrar = 30)
    {
        var reclutador = await Datos.UsuarioAsync(api, Datos.ReclutadorDigital);
        var ana = await Datos.UsuarioAsync(api, Datos.EstudianteUnah);

        await using var db = api.CrearContextoSinUsuario();
        var carrera = carreraId ?? (await db.PerfilesEstudiante.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.UsuarioId == ana.Id)).CarreraId;

        var vacante = new Vacante
        {
            EmpresaId = reclutador.EmpresaId!.Value,
            CreadaPorId = reclutador.Id,
            Titulo = $"Vacante de prueba {Guid.NewGuid():N}"[..40],
            Descripcion = "Vacante creada por una prueba automatizada para verificar las postulaciones.",
            Modalidad = Modalidad.Remota,
            Ubicacion = "Remoto",
            Cupos = cupos,
            Remunerada = true,
            IndiceMinimo = indiceMinimo,
            Estado = estado,
            FechaCierre = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(diasParaCerrar)),
            FechaPublicacion = DateTime.UtcNow,
        };
        vacante.Carreras.Add(new VacanteCarrera { CarreraId = carrera });
        db.Vacantes.Add(vacante);
        await db.SaveChangesAsync();

        vacantes.Add(vacante.Id);
        return vacante.Id;
    }

    public async Task CambiarEstadoVacanteAsync(Guid vacanteId, EstadoVacante estado)
    {
        await using var db = api.CrearContextoSinUsuario();
        await db.Vacantes.IgnoreQueryFilters().Where(v => v.Id == vacanteId)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Estado, estado));
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = api.CrearContextoSinUsuario();

        var postulaciones = await db.Postulaciones.IgnoreQueryFilters()
            .Where(p => estudiantes.Contains(p.EstudianteId) || vacantes.Contains(p.VacanteId))
            .Select(p => p.Id)
            .ToListAsync();

        // De hijas a padres: las claves foráneas no borran en cascada.
        await db.HistorialPostulacion.IgnoreQueryFilters().Where(h => postulaciones.Contains(h.PostulacionId)).ExecuteDeleteAsync();
        await db.Postulaciones.IgnoreQueryFilters().Where(p => postulaciones.Contains(p.Id)).ExecuteDeleteAsync();
        await db.Curriculums.IgnoreQueryFilters().Where(c => estudiantes.Contains(c.EstudianteId)).ExecuteDeleteAsync();
        await db.PerfilesEstudiante.IgnoreQueryFilters().Where(p => estudiantes.Contains(p.UsuarioId)).ExecuteDeleteAsync();
        await db.Usuarios.Where(u => estudiantes.Contains(u.Id)).ExecuteDeleteAsync();
        await db.VacanteCarreras.IgnoreQueryFilters().Where(vc => vacantes.Contains(vc.VacanteId)).ExecuteDeleteAsync();
        await db.Vacantes.IgnoreQueryFilters().Where(v => vacantes.Contains(v.Id)).ExecuteDeleteAsync();
    }
}
