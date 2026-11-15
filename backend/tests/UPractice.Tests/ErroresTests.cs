using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>T-04: el middleware y la validación devuelven exactamente el formato de S5.3.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class ErroresTests(ApiFixture api)
{
    [Fact]
    public async Task Validacion_devuelve_400_con_errores_por_campo_en_camelCase()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { correo = "no-es-correo", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://upractice.app/errores/validacion", json.GetProperty("type").GetString());
        Assert.Equal("Uno o más campos no son válidos.", json.GetProperty("title").GetString());
        Assert.Equal("/api/v1/auth/login", json.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("traceId").GetString()));

        var errores = json.GetProperty("errors");
        Assert.Equal("Ingrese un correo válido.", errores.GetProperty("correo")[0].GetString());
        Assert.Equal("Ingrese su contraseña.", errores.GetProperty("password")[0].GetString());
    }

    [Fact]
    public async Task Json_mal_formado_devuelve_el_mismo_formato_400()
    {
        var contenido = new StringContent("{ \"correo\": ", Encoding.UTF8, "application/json");
        var respuesta = await api.CreateClient().PostAsync("/api/v1/auth/login", contenido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://upractice.app/errores/validacion", json.GetProperty("type").GetString());
        Assert.True(json.GetProperty("errors").EnumerateObject().Any());
    }

    [Fact]
    public async Task Sin_token_devuelve_401_en_formato_ProblemDetails()
    {
        var respuesta = await api.CreateClient().GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://upractice.app/errores/no-autenticado", json.GetProperty("type").GetString());
    }

    [Fact]
    public async Task No_encontrado_devuelve_404_en_formato_ProblemDetails()
    {
        var cliente = await api.ClienteAutenticadoAsync(Datos.ReclutadorDigital, ApiFixture.PasswordDemo);
        var respuesta = await cliente.GetAsync($"/api/v1/pruebas/vacantes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://upractice.app/errores/no-encontrado", json.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Health_responde_sin_token()
    {
        var respuesta = await api.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
