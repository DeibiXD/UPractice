using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>Catálogo, estudiantes y postulaciones propios del caso; no modifica el seed.</summary>
internal sealed class EscenarioExploracionVacantes : IAsyncDisposable
{
    private readonly ApiFixture api;

    private EscenarioExploracionVacantes(ApiFixture api, EscenarioVacante datos,
        bool conPerfil, EstadoVerificacion verificacion, bool passwordTemporal)
    {
        this.api = api;
        Datos = datos;
        CarreraAlternativa = new Carrera { Nombre = $"Carrera alternativa {Guid.NewGuid():N}", FacultadId = datos.FacultadA.Id };
        EstudianteA = Estudiante(datos.UniversidadA.Id, datos.CarreraA.Id, conPerfil, verificacion, passwordTemporal);
        EstudianteB = Estudiante(datos.UniversidadB.Id, datos.CarreraB.Id, true, EstadoVerificacion.Verificado, false);
    }

    public EscenarioVacante Datos { get; }
    public Carrera CarreraAlternativa { get; }
    public Usuario EstudianteA { get; }
    public Usuario EstudianteB { get; }
    public DateOnly Hoy => api.Servicio<IFechaHonduras>().Hoy();

    public static async Task<EscenarioExploracionVacantes> CrearAsync(ApiFixture api,
        bool conPerfil = true, EstadoVerificacion verificacion = EstadoVerificacion.Verificado,
        bool passwordTemporal = false)
    {
        var datos = await EscenarioVacante.CrearAsync(api);
        var escenario = new EscenarioExploracionVacantes(api, datos, conPerfil, verificacion, passwordTemporal);
        try
        {
            await using var db = api.CrearContextoSinUsuario();
            db.Carreras.Add(escenario.CarreraAlternativa);
            db.Usuarios.AddRange(escenario.EstudianteA, escenario.EstudianteB);
            db.EstudianteHabilidades.Add(new() { EstudianteId = escenario.EstudianteA.Id, HabilidadId = datos.Habilidades[0].Id });
            await db.SaveChangesAsync();
            return escenario;
        }
        catch
        {
            await escenario.DisposeAsync();
            throw;
        }
    }

    public Task<HttpClient> ClienteAsync(bool estudianteB = false) =>
        api.ClienteAutenticadoAsync(estudianteB ? EstudianteB.Correo : EstudianteA.Correo, ApiFixture.PasswordDemo);

    public async Task<Vacante> VacanteAsync(string? titulo = null, bool empresaB = false,
        EstadoVacante estado = EstadoVacante.Publicada, int diasCierre = 10,
        Modalidad modalidad = Modalidad.Hibrida, bool remunerada = true,
        DateTime? fechaPublicacion = null, Guid[]? carrerasIds = null, decimal? indiceMinimo = 75m,
        HabilidadVacanteRequest[]? habilidades = null)
    {
        var solicitud = Datos.Solicitud(empresaB: empresaB);
        var vacante = new Vacante
        {
            EmpresaId = empresaB ? Datos.EmpresaB.Id : Datos.EmpresaA.Id,
            CreadaPorId = empresaB ? Datos.UsuarioB.Id : Datos.UsuarioA.Id,
            Titulo = titulo ?? solicitud.Titulo, Descripcion = solicitud.Descripcion,
            Modalidad = modalidad, Ubicacion = solicitud.Ubicacion, Cupos = 2,
            Remunerada = remunerada, IndiceMinimo = indiceMinimo, Estado = estado,
            FechaCierre = Hoy.AddDays(diasCierre),
            FechaPublicacion = estado == EstadoVacante.Borrador ? null : fechaPublicacion ?? DateTime.UtcNow.AddDays(-1),
        };
        foreach (var carreraId in carrerasIds ?? solicitud.CarrerasIds)
            vacante.Carreras.Add(new VacanteCarrera { VacanteId = vacante.Id, CarreraId = carreraId });
        foreach (var habilidad in habilidades ?? solicitud.Habilidades)
            vacante.Habilidades.Add(new VacanteHabilidad
            {
                VacanteId = vacante.Id, HabilidadId = habilidad.HabilidadId, EsRequerida = habilidad.EsRequerida,
            });
        await using var db = api.CrearContextoSinUsuario();
        db.Vacantes.Add(vacante);
        await db.SaveChangesAsync();
        return vacante;
    }

    public async Task HabilitacionAsync(bool empresaB, bool universidadB,
        EstadoHabilitacion estado = EstadoHabilitacion.Aprobada)
    {
        var empresaId = empresaB ? Datos.EmpresaB.Id : Datos.EmpresaA.Id;
        var universidadId = universidadB ? Datos.UniversidadB.Id : Datos.UniversidadA.Id;
        await using var db = api.CrearContextoSinUsuario();
        var habilitacion = await db.HabilitacionesEmpresa.IgnoreQueryFilters()
            .SingleOrDefaultAsync(h => h.EmpresaId == empresaId && h.UniversidadId == universidadId);
        if (habilitacion is null)
            db.HabilitacionesEmpresa.Add(new HabilitacionEmpresa
            {
                EmpresaId = empresaId, UniversidadId = universidadId, Estado = estado,
                MotivoRechazo = estado == EstadoHabilitacion.Rechazada ? "Rechazo del escenario." : null,
            });
        else
        {
            habilitacion.Estado = estado;
            habilitacion.MotivoRechazo = estado == EstadoHabilitacion.Rechazada ? "Rechazo del escenario." : null;
        }
        await db.SaveChangesAsync();
    }

    public async Task PostularAsync(Vacante vacante, bool estudianteB = false,
        EstadoPostulacion estado = EstadoPostulacion.Enviada)
    {
        var estudianteId = estudianteB ? EstudianteB.Id : EstudianteA.Id;
        var cv = new Curriculum
        {
            EstudianteId = estudianteId, NombreVersion = "CV de exploración", ArchivoKey = $"pruebas/{Guid.NewGuid():N}.pdf",
        };
        await using var db = api.CrearContextoSinUsuario();
        db.Curriculums.Add(cv);
        db.Postulaciones.Add(new Postulacion
        {
            EmpresaId = vacante.EmpresaId, VacanteId = vacante.Id, EstudianteId = estudianteId,
            CurriculumId = cv.Id, Estado = estado, CumpleRequisitos = true,
        });
        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using (var db = api.CrearContextoSinUsuario())
        {
            var empresasIds = new[] { Datos.EmpresaA.Id, Datos.EmpresaB.Id };
            var estudiantesIds = new[] { EstudianteA.Id, EstudianteB.Id };
            await db.HistorialPostulacion.IgnoreQueryFilters().Where(h => empresasIds.Contains(h.EmpresaId)).ExecuteDeleteAsync();
            await db.Postulaciones.IgnoreQueryFilters().Where(p => empresasIds.Contains(p.EmpresaId)).ExecuteDeleteAsync();
            await db.Curriculums.IgnoreQueryFilters().Where(c => estudiantesIds.Contains(c.EstudianteId)).ExecuteDeleteAsync();
            await db.EstudianteHabilidades.Where(h => estudiantesIds.Contains(h.EstudianteId)).ExecuteDeleteAsync();
            await db.PerfilesEstudiante.IgnoreQueryFilters().Where(p => estudiantesIds.Contains(p.UsuarioId)).ExecuteDeleteAsync();
            await db.Usuarios.Where(u => estudiantesIds.Contains(u.Id)).ExecuteDeleteAsync();
            await db.Vacantes.IgnoreQueryFilters().Where(v => empresasIds.Contains(v.EmpresaId)).ExecuteDeleteAsync();
            await db.Carreras.Where(c => c.Id == CarreraAlternativa.Id).ExecuteDeleteAsync();
        }
        await Datos.DisposeAsync();
    }

    private Usuario Estudiante(Guid universidadId, Guid carreraId, bool conPerfil,
        EstadoVerificacion verificacion, bool passwordTemporal)
    {
        var estudiante = new Usuario
        {
            Correo = $"exploracion-{Guid.NewGuid():N}@vacantes.example.test",
            Nombre1 = "Estudiante", Apellido1 = "Exploración", Rol = Rol.Estudiante,
            UniversidadId = universidadId, PasswordHash = Datos.UsuarioA.PasswordHash,
            DebeCambiarPassword = passwordTemporal,
        };
        if (conPerfil)
            estudiante.Perfil = new PerfilEstudiante
            {
                UsuarioId = estudiante.Id, CarreraId = carreraId, NumeroCuenta = Guid.NewGuid().ToString("N")[..16],
                IndiceAcademico = 85m, MostrarIndice = false, EstadoVerificacion = verificacion,
                FechaVerificacion = verificacion == EstadoVerificacion.Verificado ? DateTime.UtcNow : null,
                ObservacionVerificacion = verificacion == EstadoVerificacion.Rechazado ? "Observación propia del escenario de prueba." : null,
            };
        return estudiante;
    }
}
