using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>Vacantes y postulaciones propias del caso; conserva intacto el seed compartido.</summary>
internal sealed class EscenarioGestionVacantes : IAsyncDisposable
{
    private readonly ApiFixture api;
    private readonly List<Guid> estudiantesIds = [];

    private EscenarioGestionVacantes(ApiFixture api, EscenarioVacante datos)
    {
        this.api = api;
        Datos = datos;
        CarreraNueva = new Carrera
        {
            Nombre = $"Carrera alternativa {Guid.NewGuid():N}",
            FacultadId = datos.FacultadA.Id,
        };
    }

    public EscenarioVacante Datos { get; }
    public Carrera CarreraNueva { get; }
    public DateOnly Hoy => api.Servicio<IFechaHonduras>().Hoy();

    public static async Task<EscenarioGestionVacantes> CrearAsync(ApiFixture api,
        EstadoHabilitacion? aprobacion = EstadoHabilitacion.Aprobada,
        Rol rol = Rol.Reclutador, bool passwordTemporal = false,
        bool carreraActiva = true, bool universidadActiva = true)
    {
        var datos = await EscenarioVacante.CrearAsync(api, habilitacionA: aprobacion, rol: rol,
            passwordTemporal: passwordTemporal, carreraActiva: carreraActiva, universidadActiva: universidadActiva);
        var escenario = new EscenarioGestionVacantes(api, datos);
        try
        {
            await using var db = api.CrearContextoSinUsuario();
            db.Carreras.Add(escenario.CarreraNueva);
            await db.SaveChangesAsync();
            return escenario;
        }
        catch
        {
            await escenario.DisposeAsync();
            throw;
        }
    }

    public Task<HttpClient> ClienteAsync(bool empresaB = false) => Datos.ClienteAsync(empresaB);

    public async Task<Vacante> VacanteAsync(EstadoVacante estado = EstadoVacante.Borrador,
        int diasCierre = 10, bool empresaB = false)
    {
        var solicitud = Datos.Solicitud(empresaB: empresaB);
        var vacante = new Vacante
        {
            EmpresaId = empresaB ? Datos.EmpresaB.Id : Datos.EmpresaA.Id,
            CreadaPorId = empresaB ? Datos.UsuarioB.Id : Datos.UsuarioA.Id,
            Titulo = solicitud.Titulo,
            Descripcion = solicitud.Descripcion,
            Modalidad = solicitud.Modalidad,
            Ubicacion = solicitud.Ubicacion,
            Cupos = solicitud.Cupos,
            Remunerada = solicitud.Remunerada,
            IndiceMinimo = solicitud.IndiceMinimo,
            FechaCierre = Hoy.AddDays(diasCierre),
            Estado = estado,
            FechaPublicacion = estado == EstadoVacante.Borrador ? null : DateTime.UtcNow.AddDays(-2),
        };
        vacante.Carreras.Add(new VacanteCarrera { VacanteId = vacante.Id, CarreraId = solicitud.CarrerasIds[0] });
        foreach (var habilidad in solicitud.Habilidades)
            vacante.Habilidades.Add(new VacanteHabilidad
            {
                VacanteId = vacante.Id, HabilidadId = habilidad.HabilidadId, EsRequerida = habilidad.EsRequerida,
            });
        await using var db = api.CrearContextoSinUsuario();
        db.Vacantes.Add(vacante);
        await db.SaveChangesAsync();
        return vacante;
    }

    public ActualizarVacanteRequest Edicion() => new(
        "  Practicante de análisis de datos  ",
        "  Apoyo al equipo en el análisis de datos y la preparación de informes de seguimiento.  ",
        Modalidad.Remota, "  San Pedro Sula  ", 4, false, 82.125m, Hoy.AddDays(20),
        [CarreraNueva.Id], [new(Datos.Habilidades[1].Id, true)]);

    public async Task PostulacionesAsync(Vacante vacante, params EstadoPostulacion[] estados)
    {
        await using var db = api.CrearContextoSinUsuario();
        foreach (var estado in estados)
        {
            var estudiante = new Usuario
            {
                Correo = $"conteos-{Guid.NewGuid():N}@vacantes.example.test",
                Nombre1 = "Estudiante", Apellido1 = "Conteos", Rol = Rol.Estudiante,
                UniversidadId = Datos.UniversidadA.Id, PasswordHash = Datos.UsuarioA.PasswordHash,
            };
            estudiantesIds.Add(estudiante.Id);
            var cv = new Curriculum
            {
                EstudianteId = estudiante.Id, NombreVersion = "CV de prueba", ArchivoKey = $"pruebas/{Guid.NewGuid():N}.pdf",
            };
            db.Usuarios.Add(estudiante);
            db.Curriculums.Add(cv);
            db.Postulaciones.Add(new Postulacion
            {
                EmpresaId = vacante.EmpresaId, VacanteId = vacante.Id, EstudianteId = estudiante.Id,
                CurriculumId = cv.Id, Estado = estado, CumpleRequisitos = true,
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task<Vacante> GuardadaAsync(Guid id)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Vacantes.IgnoreQueryFilters().AsNoTracking()
            .Include(v => v.Carreras).Include(v => v.Habilidades).AsSplitQuery().SingleAsync(v => v.Id == id);
    }

    public async ValueTask DisposeAsync()
    {
        await using (var db = api.CrearContextoSinUsuario())
        {
            var empresasIds = new[] { Datos.EmpresaA.Id, Datos.EmpresaB.Id };
            await db.HistorialPostulacion.IgnoreQueryFilters().Where(h => empresasIds.Contains(h.EmpresaId)).ExecuteDeleteAsync();
            await db.Postulaciones.IgnoreQueryFilters().Where(p => empresasIds.Contains(p.EmpresaId)).ExecuteDeleteAsync();
            await db.Curriculums.IgnoreQueryFilters().Where(c => estudiantesIds.Contains(c.EstudianteId)).ExecuteDeleteAsync();
            await db.Usuarios.Where(u => estudiantesIds.Contains(u.Id)).ExecuteDeleteAsync();
            await db.Vacantes.IgnoreQueryFilters().Where(v => empresasIds.Contains(v.EmpresaId)).ExecuteDeleteAsync();
            await db.Carreras.Where(c => c.Id == CarreraNueva.Id).ExecuteDeleteAsync();
        }
        await Datos.DisposeAsync();
    }
}
