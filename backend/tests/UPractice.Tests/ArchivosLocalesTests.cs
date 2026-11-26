using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UPractice.Application.Abstracciones;
using UPractice.Tests.Infraestructura;

namespace UPractice.Tests;

/// <summary>T-08: la API registra el proveedor local y sirve sus URL firmadas en GET /api/v1/archivos/local.</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class ArchivosLocalesTests(ApiFixture api)
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% integracion T-08\n%%EOF");

    [Fact]
    public async Task La_url_firmada_descarga_el_pdf_sin_token()
    {
        var storage = api.Servicio<IAlmacenamientoArchivos>();
        var key = LlavesArchivo.Curriculum(Guid.NewGuid(), Guid.NewGuid());
        await storage.SubirAsync(key, new MemoryStream(Pdf), "application/pdf", default);
        var url = await storage.ObtenerUrlDescargaAsync(key, LlavesArchivo.VigenciaDescarga, "CV Ana.pdf", default);

        var respuesta = await api.CreateClient().GetAsync(new Uri(url.Url).PathAndQuery);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/pdf", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("CV Ana.pdf", respuesta.Content.Headers.ContentDisposition?.FileNameStar ?? respuesta.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal(Pdf, await respuesta.Content.ReadAsByteArrayAsync());

        await storage.EliminarAsync(key, default);
    }

    [Theory]
    [InlineData("?key=curriculums/a/b.pdf&exp=99999999999&nombre=x.pdf&sig=falsa")]
    [InlineData("?key=../../appsettings.json&exp=99999999999&nombre=x&sig=falsa")]
    [InlineData("")]
    public async Task Firma_invalida_o_parametros_faltantes_dan_404_problem_details(string query)
    {
        var respuesta = await api.CreateClient().GetAsync("/api/v1/archivos/local" + query);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://upractice.app/errores/no-encontrado", json.GetProperty("type").GetString());
    }
}
