using Microsoft.EntityFrameworkCore;
using UPractice.Application.Features.Empresa.Usuarios;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>Dos empresas y sus cuentas propias; limpia también las cuentas creadas por los endpoints.</summary>
internal sealed class EscenarioUsuariosEmpresa : IAsyncDisposable
{
    private readonly ApiFixture api;

    private EscenarioUsuariosEmpresa(ApiFixture api, EscenarioVacante datos)
    {
        this.api = api;
        Datos = datos;
        ReclutadorA = Usuario("María", Rol.Reclutador);
        ReclutadorA.Nombre2 = "José";
        ReclutadorA.Apellido1 = "O'Connor";
        ReclutadorA.Apellido2 = "López";
        InactivoA = Usuario("Inactivo", Rol.Reclutador);
        InactivoA.Activo = false;
        Estudiante = new Usuario
        {
            Correo = $"estudiante.{Guid.NewGuid():N}@usuarios.example.test",
            Nombre1 = "Estudiante", Apellido1 = "Prueba", Rol = Rol.Estudiante,
            UniversidadId = datos.UniversidadA.Id, PasswordHash = datos.UsuarioA.PasswordHash,
        };
        Estudiante.Perfil = new PerfilEstudiante
        {
            UsuarioId = Estudiante.Id, CarreraId = datos.CarreraA.Id,
            NumeroCuenta = Guid.NewGuid().ToString("N")[..16], IndiceAcademico = 85m,
            EstadoVerificacion = EstadoVerificacion.Verificado, FechaVerificacion = DateTime.UtcNow,
        };
    }

    public EscenarioVacante Datos { get; }
    public Usuario ReclutadorA { get; }
    public Usuario InactivoA { get; }
    public Usuario Estudiante { get; }

    public static async Task<EscenarioUsuariosEmpresa> CrearAsync(ApiFixture api,
        bool passwordTemporal = false, bool habilitada = true)
    {
        var datos = await EscenarioVacante.CrearAsync(api, rol: Rol.AdminEmpresa,
            passwordTemporal: passwordTemporal,
            habilitacionA: habilitada ? EstadoHabilitacion.Aprobada : null);
        var escenario = new EscenarioUsuariosEmpresa(api, datos);
        try
        {
            await using var db = api.CrearContextoSinUsuario();
            await db.Usuarios.Where(u => u.Id == datos.UsuarioB.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Rol, Rol.AdminEmpresa));
            datos.UsuarioB.Rol = Rol.AdminEmpresa;
            db.Usuarios.AddRange(escenario.ReclutadorA, escenario.InactivoA, escenario.Estudiante);
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

    public CrearUsuarioEmpresaRequest Solicitud(Rol rol = Rol.Reclutador, string? correo = null) => new(
        "  Ana-María  ", "  Lucía  ", "  O'Connor  ", "  López  ",
        correo ?? $"  NUEVA.{Guid.NewGuid():N}@USUARIOS.EXAMPLE.TEST  ", rol);

    public async Task<Usuario> CuentaAsync(Guid id)
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == id);
    }

    public async Task<int> TotalCuentasAsync()
    {
        var empresasIds = new[] { Datos.EmpresaA.Id, Datos.EmpresaB.Id };
        await using var db = api.CrearContextoSinUsuario();
        return await db.Usuarios.CountAsync(u => empresasIds.Contains(u.EmpresaId ?? Guid.Empty));
    }

    public async Task<(Guid VacanteId, Guid PostulacionId)> RecursoAsync(bool empresaB = false)
    {
        var vacante = new Vacante
        {
            EmpresaId = empresaB ? Datos.EmpresaB.Id : Datos.EmpresaA.Id,
            CreadaPorId = empresaB ? Datos.UsuarioB.Id : Datos.UsuarioA.Id,
            Titulo = "Vacante propia de aislamiento de cuentas",
            Descripcion = "Vacante de prueba para comprobar permisos sobre los recursos reales de cada empresa.",
            Modalidad = Modalidad.Remota, Ubicacion = "Tegucigalpa", Cupos = 2,
            Estado = EstadoVacante.Publicada, FechaCierre = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            FechaPublicacion = DateTime.UtcNow,
        };
        vacante.Carreras.Add(new()
        {
            VacanteId = vacante.Id, CarreraId = empresaB ? Datos.CarreraB.Id : Datos.CarreraA.Id,
        });
        var cv = new Curriculum
        {
            EstudianteId = Estudiante.Id, NombreVersion = "CV de prueba", ArchivoKey = $"pruebas/{Guid.NewGuid():N}.pdf",
        };
        var postulacion = new Postulacion
        {
            EmpresaId = vacante.EmpresaId, VacanteId = vacante.Id,
            EstudianteId = Estudiante.Id, CurriculumId = cv.Id, Estado = EstadoPostulacion.Enviada,
        };
        await using var db = api.CrearContextoSinUsuario();
        db.Vacantes.Add(vacante);
        db.Curriculums.Add(cv);
        db.Postulaciones.Add(postulacion);
        await db.SaveChangesAsync();
        return (vacante.Id, postulacion.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await using (var db = api.CrearContextoSinUsuario())
        {
            var empresasIds = new[] { Datos.EmpresaA.Id, Datos.EmpresaB.Id };
            await db.HistorialPostulacion.IgnoreQueryFilters().Where(h => empresasIds.Contains(h.EmpresaId)).ExecuteDeleteAsync();
            await db.Postulaciones.IgnoreQueryFilters().Where(p => empresasIds.Contains(p.EmpresaId)).ExecuteDeleteAsync();
            await db.Curriculums.IgnoreQueryFilters().Where(c => c.EstudianteId == Estudiante.Id).ExecuteDeleteAsync();
            await db.PerfilesEstudiante.IgnoreQueryFilters().Where(p => p.UsuarioId == Estudiante.Id).ExecuteDeleteAsync();
            await db.Usuarios.Where(u => u.Id == Estudiante.Id).ExecuteDeleteAsync();
            await db.Vacantes.IgnoreQueryFilters().Where(v => empresasIds.Contains(v.EmpresaId)).ExecuteDeleteAsync();
            var actoresIds = new[] { Datos.UsuarioA.Id, Datos.UsuarioB.Id };
            await db.Usuarios.Where(u => empresasIds.Contains(u.EmpresaId ?? Guid.Empty) && !actoresIds.Contains(u.Id)).ExecuteDeleteAsync();
        }
        await Datos.DisposeAsync();
    }

    private Usuario Usuario(string nombre, Rol rol) => new()
    {
        Correo = $"cuenta.{Guid.NewGuid():N}@usuarios.example.test",
        Nombre1 = nombre, Apellido1 = "Prueba", Rol = rol, EmpresaId = Datos.EmpresaA.Id,
        PasswordHash = Datos.UsuarioA.PasswordHash,
    };
}
