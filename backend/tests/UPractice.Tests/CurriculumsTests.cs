using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UPractice.Domain.Enums;
using UPractice.Tests.Infraestructura;
using static UPractice.Tests.PerfilEstudianteTests;

namespace UPractice.Tests;

/// <summary>HU-03: versiones del CV con el almacenamiento local de T-08 (S3.8).</summary>
[Collection(ColeccionApi.Nombre)]
public sealed class CurriculumsTests(ApiFixture api)
{
    private const string Ruta = "/api/v1/estudiantes/me/curriculums";
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% HU-03\n%%EOF");

    [Fact]
    public async Task Subir_la_primera_version_la_deja_principal_y_se_descarga_con_url_firmada()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);

        var respuesta = await cliente.PostAsync(Ruta, Formulario("CV desarrollo web", Pdf));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(creado.GetProperty("esPrincipal").GetBoolean());
        Assert.Equal("CV desarrollo web", creado.GetProperty("nombreVersion").GetString());
        var id = creado.GetProperty("id").GetGuid();
        var guardado = Assert.Single(await escenario.CurriculumsAsync(estudiante.Id));
        Assert.Equal($"curriculums/{estudiante.Id}/{id}.pdf", guardado.ArchivoKey);

        var url = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}/{id}/descarga");
        Assert.True(url.GetProperty("expiraEn").GetDateTime() > DateTime.UtcNow);
        var descarga = await api.CreateClient().GetAsync(new Uri(url.GetProperty("url").GetString()!).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal(Pdf, await descarga.Content.ReadAsByteArrayAsync());
        Assert.Equal("CV desarrollo web.pdf", descarga.Content.Headers.ContentDisposition?.FileNameStar ?? descarga.Content.Headers.ContentDisposition?.FileName);
    }

    [Theory]
    [InlineData("cv.txt", "application/pdf", true)]    // extensión
    [InlineData("cv.pdf", "text/plain", true)]         // tipo MIME
    [InlineData("cv.pdf", "application/pdf", false)]   // contenido sin firma %PDF
    public async Task Archivo_que_no_es_pdf_da_400_en_el_campo_archivo(string nombre, string tipo, bool firmaValida)
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);
        var contenido = firmaValida ? Pdf : Encoding.ASCII.GetBytes("<html>no soy un pdf</html>");

        var respuesta = await cliente.PostAsync(Ruta, Formulario("CV general", contenido, nombre, tipo));

        Assert.Equal("Suba un archivo PDF de máximo 5 MB.", await ErrorCampoAsync(respuesta, "archivo"));
        Assert.Empty(await escenario.CurriculumsAsync(estudiante.Id));
    }

    [Fact]
    public async Task Pdf_de_mas_de_5_MB_o_sin_archivo_o_nombre_corto_dan_400()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);
        var grande = new byte[5 * 1024 * 1024 + 1];
        Pdf.CopyTo(grande, 0);

        Assert.Equal("Suba un archivo PDF de máximo 5 MB.", await ErrorCampoAsync(await cliente.PostAsync(Ruta, Formulario("CV general", grande)), "archivo"));

        var sinArchivo = new MultipartFormDataContent { { new StringContent("CV general"), "nombreVersion" } };
        Assert.Equal("Suba un archivo PDF de máximo 5 MB.", await ErrorCampoAsync(await cliente.PostAsync(Ruta, sinArchivo), "archivo"));

        Assert.Equal("El nombre de la versión debe tener entre 3 y 60 caracteres.",
            await ErrorCampoAsync(await cliente.PostAsync(Ruta, Formulario("ab", Pdf)), "nombreVersion"));
    }

    [Fact]
    public async Task Maximo_5_versiones_activas()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);
        for (var i = 1; i <= 5; i++)
            Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsync(Ruta, Formulario($"Versión {i}", Pdf))).StatusCode);

        var sexta = await cliente.PostAsync(Ruta, Formulario("Versión 6", Pdf));

        await ProblemaAsync(sexta, HttpStatusCode.UnprocessableEntity, "LIMITE_CURRICULUMS");
        Assert.Equal(5, (await escenario.CurriculumsAsync(estudiante.Id)).Count);
    }

    [Fact]
    public async Task Cambiar_principal_y_eliminar_la_principal_promueve_la_mas_reciente()
    {
        await using var escenario = new EscenarioPerfil(api);
        var estudiante = await escenario.CrearEstudianteAsync();
        using var cliente = await Cliente(estudiante.Correo);
        var a = await SubirAsync(cliente, "Versión A");
        var b = await SubirAsync(cliente, "Versión B");
        var c = await SubirAsync(cliente, "Versión C");

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PatchAsync($"{Ruta}/{b}/principal", null)).StatusCode);
        var lista = await cliente.GetFromJsonAsync<JsonElement>(Ruta);
        Assert.Equal(b, lista[0].GetProperty("id").GetGuid());   // la principal primero
        Assert.Single(lista.EnumerateArray(), x => x.GetProperty("esPrincipal").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"{Ruta}/{b}")).StatusCode);
        lista = await cliente.GetFromJsonAsync<JsonElement>(Ruta);
        Assert.Equal(2, lista.GetArrayLength());
        Assert.Equal(c, lista[0].GetProperty("id").GetGuid());
        Assert.True(lista[0].GetProperty("esPrincipal").GetBoolean());

        // Baja lógica: la fila sigue, marcada como eliminada; ya no se descarga ni se puede marcar.
        Assert.True((await escenario.CurriculumsAsync(estudiante.Id)).Single(x => x.Id == b).Eliminado);
        await ProblemaAsync(await cliente.GetAsync($"{Ruta}/{b}/descarga"), HttpStatusCode.NotFound);
        await ProblemaAsync(await cliente.PatchAsync($"{Ruta}/{b}/principal", null), HttpStatusCode.NotFound);
        Assert.NotEqual(Guid.Empty, a);
    }

    [Fact]
    public async Task El_cv_de_otro_estudiante_no_existe()
    {
        await using var escenario = new EscenarioPerfil(api);
        var duena = await escenario.CrearEstudianteAsync();
        var otra = await escenario.CrearEstudianteAsync();
        using var clienteDuena = await Cliente(duena.Correo);
        var id = await SubirAsync(clienteDuena, "CV privado");

        using var clienteOtra = await Cliente(otra.Correo);
        await ProblemaAsync(await clienteOtra.GetAsync($"{Ruta}/{id}/descarga"), HttpStatusCode.NotFound);
        await ProblemaAsync(await clienteOtra.DeleteAsync($"{Ruta}/{id}"), HttpStatusCode.NotFound);
        Assert.Equal(0, (await clienteOtra.GetFromJsonAsync<JsonElement>(Ruta)).GetArrayLength());
    }

    [Fact]
    public async Task La_empresa_descarga_el_cv_adjunto_aunque_el_estudiante_lo_elimine_y_otra_empresa_recibe_404()
    {
        await using var postulaciones = new EscenarioPostulaciones(api);
        var estudiante = await postulaciones.CrearEstudianteAsync();
        var vacanteId = await postulaciones.CrearVacanteAsync();
        using var cliente = await Cliente(estudiante.Correo);
        var cv = await SubirAsync(cliente, "CV para la vacante");

        var postular = await cliente.PostAsJsonAsync($"/api/v1/vacantes/{vacanteId}/postulaciones", new { curriculumId = cv });
        Assert.Equal(HttpStatusCode.Created, postular.StatusCode);
        var postulacionId = (await postular.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"{Ruta}/{cv}")).StatusCode);

        var ruta = $"/api/v1/empresa/postulaciones/{postulacionId}/curriculum/descarga";
        using var digital = await Cliente(Datos.ReclutadorDigital);
        var url = await digital.GetFromJsonAsync<JsonElement>(ruta);
        var descarga = await api.CreateClient().GetAsync(new Uri(url.GetProperty("url").GetString()!).PathAndQuery);
        Assert.Equal(Pdf, await descarga.Content.ReadAsByteArrayAsync());

        using var finanzas = await Cliente(Datos.ReclutadorFinanzas);
        await ProblemaAsync(await finanzas.GetAsync(ruta), HttpStatusCode.NotFound);
    }

    private static async Task<Guid> SubirAsync(HttpClient cliente, string nombre)
    {
        var respuesta = await cliente.PostAsync(Ruta, Formulario(nombre, Pdf));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent Formulario(string nombreVersion, byte[] contenido,
        string nombreArchivo = "cv.pdf", string tipo = "application/pdf")
    {
        var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        return new MultipartFormDataContent
        {
            { new StringContent(nombreVersion), "nombreVersion" },
            { archivo, "archivo", nombreArchivo },
        };
    }

    private Task<HttpClient> Cliente(string correo) => api.ClienteAutenticadoAsync(correo, ApiFixture.PasswordDemo);
}
