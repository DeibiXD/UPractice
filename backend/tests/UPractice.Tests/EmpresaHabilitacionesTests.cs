using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Features.Empresa.Habilitaciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

[Collection(ColeccionApi.Nombre)]
public sealed class EmpresaHabilitacionesTests(ApiFixture api)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Registro_crea_empresa_admin_y_habilitaciones_pendientes_y_permite_login()
    {
        var universidadId = await UniversidadActivaAsync();
        var rtn = NuevoRtn();
        var correo = $"admin-{Guid.NewGuid():N}@empresa.test";
        var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/registro/empresa", Registro(rtn, correo, universidadId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/auth/me", response.Headers.Location?.OriginalString);
        var registro = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AdminEmpresa", registro.GetProperty("usuario").GetProperty("rol").GetString());
        Assert.False(string.IsNullOrEmpty(registro.GetProperty("accessToken").GetString()));

        var cliente = await api.ClienteAutenticadoAsync(correo, "Empresa-Prueba-2026");
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/auth/me")).StatusCode);

        await using var db = api.CrearContextoSinUsuario();
        var empresa = await db.Empresas.SingleAsync(e => e.Rtn == rtn);
        var admin = await db.Usuarios.SingleAsync(u => u.Correo == correo);
        var habilitaciones = await db.HabilitacionesEmpresa.IgnoreQueryFilters()
            .Where(h => h.EmpresaId == empresa.Id).ToListAsync();

        Assert.Equal(empresa.Id, admin.EmpresaId);
        Assert.Equal(Rol.AdminEmpresa, admin.Rol);
        Assert.NotEqual("Empresa-Prueba-2026", admin.PasswordHash);
        Assert.Collection(habilitaciones, h => Assert.Equal(EstadoHabilitacion.Pendiente, h.Estado));
    }

    [Fact]
    public async Task Registro_rechaza_rtn_duplicado_con_codigo_RTN_EN_USO()
    {
        var universidadId = await UniversidadActivaAsync();
        var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/registro/empresa",
            Registro("08019020000011", $"otro-{Guid.NewGuid():N}@empresa.test", universidadId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("RTN_EN_USO", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Admin_empresa_solo_lista_sus_habilitaciones()
    {
        var digital = await api.ClienteAutenticadoAsync("admin@digitales.example.com", ApiFixture.PasswordDemo);
        var finanzas = await api.ClienteAutenticadoAsync("admin@finanzas.example.com", ApiFixture.PasswordDemo);

        var digitales = await digital.GetFromJsonAsync<List<HabilitacionEmpresaDto>>(
            "/api/v1/empresa/habilitaciones", JsonOptions);
        var financieras = await finanzas.GetFromJsonAsync<List<HabilitacionEmpresaDto>>(
            "/api/v1/empresa/habilitaciones", JsonOptions);

        Assert.NotEmpty(digitales!);
        Assert.NotEmpty(financieras!);
        await using var db = api.CrearContextoSinUsuario();
        var empresaDigitalId = await db.Usuarios.Where(u => u.Correo == "admin@digitales.example.com")
            .Select(u => u.EmpresaId).SingleAsync();
        var empresaFinanzasId = await db.Usuarios.Where(u => u.Correo == "admin@finanzas.example.com")
            .Select(u => u.EmpresaId).SingleAsync();
        var idsDigitales = await db.HabilitacionesEmpresa.IgnoreQueryFilters()
            .Where(h => h.EmpresaId == empresaDigitalId).Select(h => h.Id).ToHashSetAsync();
        var idsFinanzas = await db.HabilitacionesEmpresa.IgnoreQueryFilters()
            .Where(h => h.EmpresaId == empresaFinanzasId).Select(h => h.Id).ToHashSetAsync();
        Assert.True(idsDigitales.SetEquals(digitales!.Select(h => h.Id)));
        Assert.True(idsFinanzas.SetEquals(financieras!.Select(h => h.Id)));
        Assert.Empty(idsDigitales.Intersect(idsFinanzas));

        var universidadNueva = new Universidad
        {
            Nombre = "Universidad de aislamiento",
            Siglas = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            DominioCorreo = $"{Guid.NewGuid():N}.test",
        };
        db.Universidades.Add(universidadNueva);
        await db.SaveChangesAsync();
        var solicitudAjena = await digital.PostAsJsonAsync("/api/v1/empresa/habilitaciones", new
        {
            universidadId = universidadNueva.Id,
            empresaId = empresaFinanzasId,
        });
        Assert.Equal(HttpStatusCode.Created, solicitudAjena.StatusCode);

        var asignada = await db.HabilitacionesEmpresa.IgnoreQueryFilters()
            .SingleAsync(h => h.UniversidadId == universidadNueva.Id);
        Assert.Equal(empresaDigitalId, asignada.EmpresaId);
    }

    [Fact]
    public async Task Solicitud_pendiente_rechaza_duplicado_y_permite_reintentar_tras_rechazo()
    {
        var universidadId = await UniversidadActivaAsync();
        var rtn = NuevoRtn();
        var correo = $"admin-{Guid.NewGuid():N}@empresa.test";
        var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/registro/empresa", Registro(rtn, correo, universidadId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        var empresaId = session.GetProperty("usuario").GetProperty("empresa").GetProperty("id").GetGuid();
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());

        var solicitud = await cliente.PostAsJsonAsync("/api/v1/empresa/habilitaciones", new { universidadId });
        Assert.Equal(HttpStatusCode.Conflict, solicitud.StatusCode);

        await using (var db = api.CrearContextoSinUsuario())
        {
            var habilitacion = await db.HabilitacionesEmpresa.IgnoreQueryFilters()
                .SingleAsync(h => h.EmpresaId == empresaId && h.UniversidadId == universidadId);
            habilitacion.Estado = EstadoHabilitacion.Rechazada;
            habilitacion.MotivoRechazo = "Documentación incompleta.";
            habilitacion.FechaResolucion = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var listado = await cliente.GetFromJsonAsync<List<HabilitacionEmpresaDto>>(
            "/api/v1/empresa/habilitaciones", JsonOptions);
        Assert.Equal("Documentación incompleta.", Assert.Single(listado!).MotivoRechazo);

        var reintento = await cliente.PostAsJsonAsync("/api/v1/empresa/habilitaciones", new { universidadId });
        Assert.Equal(HttpStatusCode.Created, reintento.StatusCode);
        var nuevaSolicitud = await reintento.Content.ReadFromJsonAsync<HabilitacionEmpresaDto>(JsonOptions);
        Assert.Equal(EstadoHabilitacion.Pendiente, nuevaSolicitud!.Estado);
        Assert.Null(nuevaSolicitud.MotivoRechazo);
    }

    [Fact]
    public async Task Empresa_sin_habilitacion_aprobada_recibe_422_al_validar_publicacion()
    {
        var universidadId = await UniversidadActivaAsync();
        var correo = $"admin-{Guid.NewGuid():N}@empresa.test";
        var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/registro/empresa", Registro(NuevoRtn(), correo, universidadId));
        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        var empresaId = session.GetProperty("usuario").GetProperty("empresa").GetProperty("id").GetGuid();
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new("Bearer", session!.GetProperty("accessToken").GetString());

        var rechazo = await cliente.PostAsync("/api/v1/pruebas/publicar", null);

        Assert.Equal((HttpStatusCode)422, rechazo.StatusCode);
        var problem = await rechazo.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMPRESA_NO_HABILITADA", problem.GetProperty("code").GetString());

        await using (var db = api.CrearContextoSinUsuario())
        {
            var habilitacion = await db.HabilitacionesEmpresa.IgnoreQueryFilters()
                .SingleAsync(h => h.EmpresaId == empresaId && h.UniversidadId == universidadId);
            habilitacion.Estado = EstadoHabilitacion.Aprobada;
            habilitacion.MotivoRechazo = null;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.NoContent,
            (await cliente.PostAsync("/api/v1/pruebas/publicar", null)).StatusCode);
    }

    [Fact]
    public async Task OpenApi_incluye_los_cuatro_endpoints_y_sus_respuestas()
    {
        var json = await api.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = json.GetProperty("paths");
        var registro = paths.GetProperty("/api/v1/auth/registro/empresa").GetProperty("post");
        var solicitud = paths.GetProperty("/api/v1/empresa/habilitaciones").GetProperty("post");

        Assert.Equal("Registra una empresa y solicita sus habilitaciones iniciales",
            registro.GetProperty("summary").GetString());
        Assert.True(registro.GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(registro.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(registro.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(paths.GetProperty("/api/v1/catalogos/universidades").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/v1/empresa/habilitaciones").TryGetProperty("get", out _));
        Assert.True(solicitud.GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(solicitud.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(solicitud.GetProperty("responses").TryGetProperty("409", out _));
    }

    [Fact]
    public async Task Registro_invalido_devuelve_400()
    {
        var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/registro/empresa",
            Registro("123", $"admin-{Guid.NewGuid():N}@empresa.test", Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> UniversidadActivaAsync()
    {
        await using var db = api.CrearContextoSinUsuario();
        return await db.Universidades.Where(u => u.Activa).Select(u => u.Id).FirstAsync();
    }

    private static object Registro(string rtn, string correo, Guid universidadId) => new
    {
        nombreComercial = "Empresa de prueba",
        razonSocial = "Empresa de prueba S. de R.L.",
        rtn,
        sector = "Tecnología",
        universidadesIds = new[] { universidadId },
        admin = new
        {
            nombre1 = "María",
            apellido1 = "Prueba",
            correo,
            password = "Empresa-Prueba-2026",
            confirmarPassword = "Empresa-Prueba-2026",
        },
    };

    private static string NuevoRtn() => $"080{Random.Shared.NextInt64(0, 100_000_000_000):D11}";
}
