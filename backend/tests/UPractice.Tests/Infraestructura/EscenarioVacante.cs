using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Vacantes;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Tests.Infraestructura;

/// <summary>Datos propios del caso; nunca modifica el seed compartido y elimina sus vacantes al terminar.</summary>
internal sealed class EscenarioVacante : IAsyncDisposable
{
    private readonly ApiFixture api;
    private readonly Guid[] empresasIds;
    private readonly string sufijo = Guid.NewGuid().ToString("N")[..10];

    private EscenarioVacante(ApiFixture api, Rol rol, bool passwordTemporal, bool carreraActiva, bool universidadActiva)
    {
        this.api = api;
        UniversidadA = Universidad("A", universidadActiva);
        UniversidadB = Universidad("B", true);
        FacultadA = new Facultad { Nombre = "Ingeniería de prueba", Universidad = UniversidadA };
        FacultadB = new Facultad { Nombre = "Ingeniería de prueba", Universidad = UniversidadB };
        CarreraA = new Carrera { Nombre = "Sistemas de prueba", Facultad = FacultadA, Activa = carreraActiva };
        CarreraB = new Carrera { Nombre = "Sistemas de prueba", Facultad = FacultadB };
        EmpresaA = Empresa("A");
        EmpresaB = Empresa("B");
        empresasIds = [EmpresaA.Id, EmpresaB.Id];
        var hash = api.Servicio<IHasherPassword>().Hashear(ApiFixture.PasswordDemo);
        UsuarioA = Cuenta("a", "Ana", rol, EmpresaA, hash, passwordTemporal);
        UsuarioB = Cuenta("b", "Beatriz", Rol.Reclutador, EmpresaB, hash, false);
        Habilidades = [new Habilidad { Nombre = $"Habilidad requerida {sufijo}" }, new Habilidad { Nombre = $"Habilidad deseable {sufijo}" }];
    }

    public Universidad UniversidadA { get; }
    public Universidad UniversidadB { get; }
    public Facultad FacultadA { get; }
    public Facultad FacultadB { get; }
    public Carrera CarreraA { get; }
    public Carrera CarreraB { get; }
    public Empresa EmpresaA { get; }
    public Empresa EmpresaB { get; }
    public Usuario UsuarioA { get; }
    public Usuario UsuarioB { get; }
    public Habilidad[] Habilidades { get; }

    public static async Task<EscenarioVacante> CrearAsync(ApiFixture api,
        EstadoHabilitacion? habilitacionA = EstadoHabilitacion.Aprobada,
        Rol rol = Rol.Reclutador, bool passwordTemporal = false,
        bool carreraActiva = true, bool universidadActiva = true)
    {
        var escenario = new EscenarioVacante(api, rol, passwordTemporal, carreraActiva, universidadActiva);
        try
        {
            await using var db = api.CrearContextoSinUsuario();
            db.Carreras.AddRange(escenario.CarreraA, escenario.CarreraB);
            db.Usuarios.AddRange(escenario.UsuarioA, escenario.UsuarioB);
            db.Habilidades.AddRange(escenario.Habilidades);
            if (habilitacionA is { } estado)
                db.HabilitacionesEmpresa.Add(Habilitacion(escenario.EmpresaA, escenario.UniversidadA, estado));
            db.HabilitacionesEmpresa.Add(Habilitacion(escenario.EmpresaB, escenario.UniversidadB, EstadoHabilitacion.Aprobada));
            await db.SaveChangesAsync();
            return escenario;
        }
        catch
        {
            await escenario.DisposeAsync();
            throw;
        }
    }

    public CrearVacanteRequest Solicitud(bool publicar = false, bool empresaB = false) => new(
        $"Practicante de desarrollo {sufijo}",
        "Apoyo al equipo de desarrollo en APIs, pruebas automatizadas y documentación técnica.",
        Modalidad.Hibrida, "Tegucigalpa", 2, true, 75m,
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
        [empresaB ? CarreraB.Id : CarreraA.Id],
        [new(Habilidades[0].Id, true), new(Habilidades[1].Id, false)], publicar);

    public Task<HttpClient> ClienteAsync(bool empresaB = false) =>
        api.ClienteAutenticadoAsync(empresaB ? UsuarioB.Correo : UsuarioA.Correo, ApiFixture.PasswordDemo);

    public async Task<bool> TieneVacantesAsync()
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Vacantes.IgnoreQueryFilters().AnyAsync(v => empresasIds.Contains(v.EmpresaId));
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = api.CrearContextoSinUsuario();
        // Las tablas puente tienen cascade. No se crean postulaciones en estos escenarios.
        await db.Vacantes.IgnoreQueryFilters().Where(v => empresasIds.Contains(v.EmpresaId)).ExecuteDeleteAsync();
        await db.HabilitacionesEmpresa.IgnoreQueryFilters().Where(h => empresasIds.Contains(h.EmpresaId)).ExecuteDeleteAsync();
        var usuariosIds = new[] { UsuarioA.Id, UsuarioB.Id };
        await db.Usuarios.Where(u => usuariosIds.Contains(u.Id)).ExecuteDeleteAsync();
        await db.Empresas.Where(e => empresasIds.Contains(e.Id)).ExecuteDeleteAsync();
        var carrerasIds = new[] { CarreraA.Id, CarreraB.Id };
        await db.Carreras.Where(c => carrerasIds.Contains(c.Id)).ExecuteDeleteAsync();
        var facultadesIds = new[] { FacultadA.Id, FacultadB.Id };
        await db.Facultades.Where(f => facultadesIds.Contains(f.Id)).ExecuteDeleteAsync();
        var universidadesIds = new[] { UniversidadA.Id, UniversidadB.Id };
        await db.Universidades.Where(u => universidadesIds.Contains(u.Id)).ExecuteDeleteAsync();
        var habilidadesIds = Habilidades.Select(h => h.Id).ToArray();
        await db.Habilidades.Where(h => habilidadesIds.Contains(h.Id)).ExecuteDeleteAsync();
    }

    private Universidad Universidad(string letra, bool activa) => new()
    {
        Nombre = $"Universidad {letra} de prueba {sufijo}", Siglas = $"{letra}{sufijo}".ToUpperInvariant(),
        DominioCorreo = $"{letra.ToLowerInvariant()}-{sufijo}.example.test", Activa = activa,
    };

    private Empresa Empresa(string letra) => new()
    {
        NombreComercial = $"Empresa {letra} de prueba {sufijo}", RazonSocial = $"Empresa {letra} de prueba S.A.",
        Rtn = Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString(CultureInfo.InvariantCulture),
        Sector = "Pruebas de desarrollo",
    };

    private Usuario Cuenta(string letra, string nombre, Rol rol, Empresa empresa, string hash, bool temporal) => new()
    {
        Correo = $"{letra}.{sufijo}@vacantes.example.test", Nombre1 = nombre, Apellido1 = "Vacantes",
        Rol = rol, Empresa = empresa, PasswordHash = hash, DebeCambiarPassword = temporal,
    };

    private static HabilitacionEmpresa Habilitacion(Empresa empresa, Universidad universidad, EstadoHabilitacion estado) => new()
    {
        Empresa = empresa, Universidad = universidad, Estado = estado,
        MotivoRechazo = estado == EstadoHabilitacion.Rechazada ? "Rechazo del escenario de pruebas." : null,
    };
}
