using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UPractice.Tests.Infraestructura;

/// <summary>Aserciones comunes sobre ProblemDetails (S5.3).</summary>
internal static class RespuestasApi
{
    public static async Task<JsonElement> ProblemaAsync(HttpResponseMessage respuesta, HttpStatusCode status, string? code = null)
    {
        Assert.Equal(status, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        if (code is not null) Assert.Equal(code, json.GetProperty("code").GetString());
        return json;
    }

    public static async Task<string?> ErrorCampoAsync(HttpResponseMessage respuesta, string campo) =>
        (await ProblemaAsync(respuesta, HttpStatusCode.BadRequest)).GetProperty("errors").GetProperty(campo)[0].GetString();
}
