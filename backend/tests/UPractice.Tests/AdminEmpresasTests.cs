using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>HU-A04: el SuperAdmin supervisa empresas y las suspende o reactiva.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class AdminEmpresasTests(ApiFixture api)
{
    private const string Ruta = "/api/v1/admin/empresas";

    [Fact]
    public async Task Lista_empresas_con_rtn_sector_estado_y_habilitaciones_y_busca_por_rtn()
    {
        await using var admin = new EscenarioAdmin(api);
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await admin.ClienteSuperAdminAsync();
        var empresa = escenario.Datos.EmpresaA;

        var json = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}?busqueda={empresa.Rtn}");

        Assert.Equal(1, json.GetProperty("totalRegistros").GetInt32());
        var fila = json.GetProperty("items")[0];
        Assert.Equal(empresa.Id, fila.GetProperty("id").GetGuid());
        Assert.Equal(empresa.Rtn, fila.GetProperty("rtn").GetString());
        Assert.Equal(empresa.Sector, fila.GetProperty("sector").GetString());
        Assert.True(fila.GetProperty("activa").GetBoolean());
        Assert.True(fila.GetProperty("creadoEn").GetDateTime() > DateTime.MinValue);
        Assert.Contains(fila.GetProperty("habilitaciones").EnumerateArray(), h => h.GetProperty("estado").GetString() == "Aprobada");

        // Sin filtros aparecen también las empresas del seed.
        var todas = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}?tamanoPagina=50");
        Assert.Contains(todas.GetProperty("items").EnumerateArray(), e => e.GetProperty("nombreComercial").GetString() == "Soluciones Digitales HN");
    }

    [Fact]
    public async Task Suspender_bloquea_el_login_y_oculta_las_vacantes_y_reactivar_lo_restaura()
    {
        await using var admin = new EscenarioAdmin(api);
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await admin.ClienteSuperAdminAsync();
        var empresa = escenario.Datos.EmpresaA;
        var vacante = await escenario.VacanteAsync();
        using var estudiante = await escenario.ClienteAsync();
        Assert.Equal(HttpStatusCode.OK, (await estudiante.GetAsync($"/api/v1/vacantes/{vacante.Id}")).StatusCode);

        var suspender = await cliente.PatchAsJsonAsync($"{Ruta}/{empresa.Id}/estado",
            new { activa = false, motivoSuspension = "  Publicó vacantes engañosas para estudiantes.  " });
        Assert.Equal(HttpStatusCode.NoContent, suspender.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(escenario.Datos.UsuarioA.Correo)).StatusCode);
        await RespuestasApi.ProblemaAsync(await estudiante.GetAsync($"/api/v1/vacantes/{vacante.Id}"), HttpStatusCode.NotFound);
        var catalogo = await estudiante.GetFromJsonAsync<JsonElement>("/api/v1/vacantes?tamanoPagina=50");
        Assert.DoesNotContain(catalogo.GetProperty("items").EnumerateArray(), v => v.GetProperty("id").GetGuid() == vacante.Id);

        var suspendidas = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}?activa=false&busqueda={empresa.Rtn}");
        var fila = suspendidas.GetProperty("items")[0];
        Assert.False(fila.GetProperty("activa").GetBoolean());
        Assert.Equal("Publicó vacantes engañosas para estudiantes.", fila.GetProperty("motivoSuspension").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PatchAsJsonAsync($"{Ruta}/{empresa.Id}/estado", new { activa = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(escenario.Datos.UsuarioA.Correo)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await estudiante.GetAsync($"/api/v1/vacantes/{vacante.Id}")).StatusCode);
        var activa = (await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}?busqueda={empresa.Rtn}")).GetProperty("items")[0];
        Assert.Equal(JsonValueKind.Null, activa.GetProperty("motivoSuspension").ValueKind);
    }

    [Fact]
    public async Task Suspender_exige_motivo_y_un_cuerpo_explicito()
    {
        await using var admin = new EscenarioAdmin(api);
        await using var escenario = await EscenarioExploracionVacantes.CrearAsync(api);
        using var cliente = await admin.ClienteSuperAdminAsync();
        var ruta = $"{Ruta}/{escenario.Datos.EmpresaA.Id}/estado";

        Assert.Equal("Indique el motivo (10 a 500 caracteres).",
            await RespuestasApi.ErrorCampoAsync(await cliente.PatchAsJsonAsync(ruta, new { activa = false, motivoSuspension = "corto" }), "motivoSuspension"));
        Assert.Equal("Indique si activa o suspende la empresa.",
            await RespuestasApi.ErrorCampoAsync(await cliente.PatchAsJsonAsync(ruta, new { }), "activa"));
        await RespuestasApi.ProblemaAsync(await cliente.PatchAsJsonAsync($"{Ruta}/{Guid.NewGuid()}/estado", new { activa = true }), HttpStatusCode.NotFound);

        Assert.Equal(HttpStatusCode.OK, (await Login(escenario.Datos.UsuarioA.Correo)).StatusCode); // sigue activa
    }

    [Fact]
    public async Task Solo_SuperAdmin_y_sin_acceso_a_datos_de_negocio()
    {
        await using var admin = new EscenarioAdmin(api);
        using var cliente = await admin.ClienteSuperAdminAsync();
        using var reclutador = await api.ClienteAutenticadoAsync(Datos.ReclutadorDigital, ApiFixture.PasswordDemo);
        using var coordinador = await api.ClienteAutenticadoAsync(Datos.CoordinadorUnah, ApiFixture.PasswordDemo);

        Assert.Equal(HttpStatusCode.Forbidden, (await reclutador.GetAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.GetAsync(Ruta)).StatusCode);
        // D-14: el SuperAdmin no entra a vacantes ni postulaciones de las empresas.
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/empresa/vacantes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/vacantes")).StatusCode);

        var errores = (await RespuestasApi.ProblemaAsync(await cliente.GetAsync($"{Ruta}?pagina=0&tamanoPagina=99"), HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.Equal("La página debe ser mayor o igual a 1.", errores.GetProperty("pagina")[0].GetString());
        Assert.Equal("El tamaño de página debe estar entre 1 y 50.", errores.GetProperty("tamanoPagina")[0].GetString());
    }

    private Task<HttpResponseMessage> Login(string correo) =>
        api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo, password = ApiFixture.PasswordDemo });
}
