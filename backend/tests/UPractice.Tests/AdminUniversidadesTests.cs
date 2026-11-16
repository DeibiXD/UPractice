using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UPractice.Application.Features.Admin.Universidades.DTOs;
using UPractice.Application.Features.Admin.Universidades.Interfaces;
using UPractice.Domain.Entidades;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

[Collection(ColeccionApi.Nombre)]
public sealed class AdminUniversidadesTests(ApiFixture api)
{
    private const string PasswordNuevaAdmin = "Admin-Actualizada2026";
    private static readonly SemaphoreSlim BloqueoSuperAdmin = new(1, 1);
    private static string? tokenSuperAdmin;

    [Fact]
    public async Task SuperAdmin_lista_universidades_activas_e_inactivas()
    {
        var cliente = await ClienteSuperAdminAsync();
        var universidades = await cliente.GetFromJsonAsync<List<UniversidadAdminResponseDto>>(
            "/api/v1/admin/universidades");

        Assert.NotEmpty(universidades!);
        Assert.Contains(universidades!, u => u.Siglas == "UNAH" && u.Activa);
        Assert.Contains(universidades!, u => u.Siglas == "UPI" && !u.Activa);
    }

    [Theory]
    [InlineData(Datos.EstudianteUnah, ApiFixture.PasswordDemo)]
    [InlineData(Datos.CoordinadorUnah, ApiFixture.PasswordDemo)]
    [InlineData("admin@digitales.example.com", ApiFixture.PasswordDemo)]
    [InlineData(Datos.ReclutadorDigital, ApiFixture.PasswordDemo)]
    public async Task Solo_SuperAdmin_puede_listar_universidades(string correo, string password)
    {
        var cliente = await api.ClienteAutenticadoAsync(correo, password);

        var respuesta = await cliente.GetAsync("/api/v1/admin/universidades");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Sin_token_el_catalogo_administrativo_devuelve_401()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/v1/admin/universidades");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_normaliza_siglas_y_dominio_y_activa_por_defecto()
    {
        var cliente = await ClienteSuperAdminAsync();
        var request = new { nombre = " Universidad de prueba ", siglas = " aCme123 ", dominioCorreo = " Students.Case.Test " };

        var respuesta = await cliente.PostAsJsonAsync("/api/v1/admin/universidades", request);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("/api/v1/admin/universidades", respuesta.Headers.Location?.OriginalString);
        var universidad = await respuesta.Content.ReadFromJsonAsync<UniversidadAdminResponseDto>();
        Assert.Equal("Universidad de prueba", universidad!.Nombre);
        Assert.Equal("ACME123", universidad.Siglas);
        Assert.Equal("students.case.test", universidad.DominioCorreo);
        Assert.True(universidad.Activa);
    }

    [Theory]
    [InlineData("unah", "siglas-conflicto.test", "SIGLAS_EN_USO")]
    [InlineData("OTRA123", "UNAH.HN", "DOMINIO_EN_USO")]
    public async Task Crear_rechaza_siglas_y_dominio_duplicados_sin_distinguir_mayusculas(
        string siglas,
        string dominio,
        string codigoEsperado)
    {
        var cliente = await ClienteSuperAdminAsync();
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/admin/universidades",
            new { nombre = "Universidad duplicada", siglas, dominioCorreo = dominio });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal(codigoEsperado, (await respuesta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString());
    }

    [Fact]
    public async Task Actualizar_rechaza_colisiones_con_otra_universidad_y_permite_conservar_sus_datos()
    {
        var cliente = await ClienteSuperAdminAsync();
        var universidad = await CrearAsync(cliente, "Universidad objetivo", SiglasUnicas(), DominioUnico());

        var colisionSiglas = await cliente.PutAsJsonAsync($"/api/v1/admin/universidades/{universidad.Id}",
            new { nombre = "Actualizada", siglas = "uNaH", dominioCorreo = DominioUnico() });
        Assert.Equal(HttpStatusCode.Conflict, colisionSiglas.StatusCode);
        Assert.Equal("SIGLAS_EN_USO", (await colisionSiglas.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString());

        var colisionDominio = await cliente.PutAsJsonAsync($"/api/v1/admin/universidades/{universidad.Id}",
            new { nombre = "Actualizada", siglas = SiglasUnicas(), dominioCorreo = "UNITEC.EDU" });
        Assert.Equal(HttpStatusCode.Conflict, colisionDominio.StatusCode);
        Assert.Equal("DOMINIO_EN_USO", (await colisionDominio.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString());

        var propia = await cliente.PutAsJsonAsync($"/api/v1/admin/universidades/{universidad.Id}",
            new { nombre = "Nombre actualizado", siglas = universidad.Siglas.ToLowerInvariant(),
                dominioCorreo = universidad.DominioCorreo.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.OK, propia.StatusCode);
        var resultado = await propia.Content.ReadFromJsonAsync<UniversidadAdminResponseDto>();
        Assert.Equal(universidad.Id, resultado!.Id);
        Assert.Equal(universidad.Siglas, resultado.Siglas);
        Assert.Equal(universidad.DominioCorreo, resultado.DominioCorreo);
    }

    [Fact]
    public async Task Desactivar_oculta_universidad_y_bloquea_nuevos_registros()
    {
        var cliente = await ClienteSuperAdminAsync();
        var dominio = DominioUnico();
        var universidad = await CrearAsync(cliente, "Universidad a desactivar", SiglasUnicas(), dominio);

        var desactivada = await cliente.PatchAsJsonAsync(
            $"/api/v1/admin/universidades/{universidad.Id}/estado", new { activa = false });

        Assert.Equal(HttpStatusCode.OK, desactivada.StatusCode);
        Assert.False((await desactivada.Content.ReadFromJsonAsync<UniversidadAdminResponseDto>())!.Activa);

        var catalogo = await api.CreateClient().GetFromJsonAsync<List<JsonElement>>(
            "/api/v1/catalogos/universidades");
        Assert.DoesNotContain(catalogo!, u => u.GetProperty("id").GetGuid() == universidad.Id);

        var correoEstudiante = $"nuevo@{dominio}";
        var registroEstudiante = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/registro/estudiante", new
            {
                nombre1 = "Ana",
                apellido1 = "Prueba",
                correo = correoEstudiante,
                password = "Estudiante-2026",
                confirmarPassword = "Estudiante-2026",
            });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, registroEstudiante.StatusCode);
        Assert.Equal("CORREO_NO_INSTITUCIONAL",
            (await registroEstudiante.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var registroEmpresa = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/registro/empresa", new
            {
                nombreComercial = "Empresa de prueba",
                razonSocial = "Empresa de prueba S. de R.L.",
                rtn = RtnUnico(),
                sector = "Tecnología",
                universidadesIds = new[] { universidad.Id },
                admin = new
                {
                    nombre1 = "María",
                    apellido1 = "Prueba",
                    correo = $"admin-{Guid.NewGuid():N}@empresa.test",
                    password = "Empresa-Prueba-2026",
                    confirmarPassword = "Empresa-Prueba-2026",
                },
            });
        Assert.Equal(HttpStatusCode.BadRequest, registroEmpresa.StatusCode);
    }

    [Fact]
    public async Task Universidad_con_usuarios_no_puede_eliminarse_fisicamente()
    {
        Guid universidadId;
        await using (var db = api.CrearContextoSinUsuario())
        {
            universidadId = await db.Universidades
                .Where(u => u.Siglas == "UNAH").Select(u => u.Id).SingleAsync();
        }

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var repositorio = scope.ServiceProvider.GetRequiredService<IUniversidadRepository>();
            Assert.True(await repositorio.TieneUsuariosAsociadosAsync(universidadId, CancellationToken.None));
        }

        await using var contexto = api.CrearContextoSinUsuario();
        var universidad = await contexto.Universidades.SingleAsync(u => u.Id == universidadId);
        contexto.Universidades.Remove(universidad);
        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task OpenApi_documenta_las_cuatro_rutas_administrativas()
    {
        var documento = await api.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var rutas = documento.GetProperty("paths");
        var coleccion = rutas.GetProperty("/api/v1/admin/universidades");
        var actualizar = rutas.GetProperty("/api/v1/admin/universidades/{id}").GetProperty("put");
        var estado = rutas.GetProperty("/api/v1/admin/universidades/{id}/estado").GetProperty("patch");

        Assert.True(coleccion.TryGetProperty("get", out _));
        Assert.True(coleccion.TryGetProperty("post", out var crear));
        Assert.True(crear.GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(crear.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(crear.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(actualizar.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(actualizar.GetProperty("responses").TryGetProperty("404", out _));
        Assert.True(actualizar.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(estado.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(estado.GetProperty("responses").TryGetProperty("404", out _));
    }

    [Fact]
    public async Task Datos_invalidos_y_universidad_inexistente_devuelven_400_o_404()
    {
        var cliente = await ClienteSuperAdminAsync();
        var invalida = await cliente.PostAsJsonAsync("/api/v1/admin/universidades",
            new { nombre = "", siglas = "A.", dominioCorreo = "@invalido" });
        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);

        var actualizar = await cliente.PutAsJsonAsync($"/api/v1/admin/universidades/{Guid.NewGuid()}",
            new { nombre = "No existe", siglas = "NOEX123", dominioCorreo = DominioUnico() });
        Assert.Equal(HttpStatusCode.NotFound, actualizar.StatusCode);

        var estado = await cliente.PatchAsJsonAsync(
            $"/api/v1/admin/universidades/{Guid.NewGuid()}/estado", new { activa = false });
        Assert.Equal(HttpStatusCode.NotFound, estado.StatusCode);
    }

    private async Task<HttpClient> ClienteSuperAdminAsync()
    {
        await BloqueoSuperAdmin.WaitAsync();
        try
        {
            if (tokenSuperAdmin is null)
            {
                var clienteTemporal = await api.ClienteAutenticadoAsync(Datos.SuperAdmin, ApiFixture.PasswordSuperAdmin);
                var cambio = await clienteTemporal.PutAsJsonAsync("/api/v1/auth/password", new
                {
                    passwordActual = ApiFixture.PasswordSuperAdmin,
                    passwordNuevo = PasswordNuevaAdmin,
                    confirmarPassword = PasswordNuevaAdmin,
                });
                Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
                tokenSuperAdmin = (await cambio.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("accessToken").GetString();
            }
        }
        finally
        {
            BloqueoSuperAdmin.Release();
        }

        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenSuperAdmin);
        return cliente;
    }

    private static async Task<UniversidadAdminResponseDto> CrearAsync(
        HttpClient cliente,
        string nombre,
        string siglas,
        string dominio)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/admin/universidades",
            new { nombre, siglas, dominioCorreo = dominio });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<UniversidadAdminResponseDto>())!;
    }

    private static string SiglasUnicas() => $"T{Guid.NewGuid():N}"[..10].ToUpperInvariant();
    private static string DominioUnico() => $"u{Guid.NewGuid():N}.test";
    private static string RtnUnico() =>
        Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString();
}
