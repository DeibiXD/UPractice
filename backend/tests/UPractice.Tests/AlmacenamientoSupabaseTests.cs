using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UPractice.Application.Abstracciones;
using UPractice.Infrastructure.Almacenamiento;

namespace UPractice.Tests;

/// <summary>T-08: peticiones que el proveedor hace a la API REST de Supabase Storage (sin red: handler falso).</summary>
public sealed class AlmacenamientoSupabaseTests
{
    private const string Url = "https://abcd.supabase.co";
    private static readonly string Key = LlavesArchivo.Curriculum(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private readonly RelojManual reloj = new(new DateTimeOffset(2026, 11, 21, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Subir_envia_el_pdf_al_bucket_privado_con_las_credenciales()
    {
        var (storage, handler) = Crear("sb_secret_abc123");

        await storage.SubirAsync(Key, new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4")), "application/pdf", default);

        var p = Assert.Single(handler.Peticiones);
        Assert.Equal(HttpMethod.Post, p.Metodo);
        Assert.Equal($"{Url}/storage/v1/object/upractice-archivos/{Key}", p.Url);
        Assert.Equal("sb_secret_abc123", p.Encabezados["apikey"]);
        Assert.False(p.Encabezados.ContainsKey("Authorization")); // la clave nueva no es un JWT
        Assert.Equal("true", p.Encabezados["x-upsert"]);
        Assert.Equal("application/pdf", p.ContentType);
        Assert.Equal("%PDF-1.4", p.Cuerpo);
    }

    [Fact]
    public async Task La_clave_heredada_service_role_tambien_va_como_bearer()
    {
        var (storage, handler) = Crear("eyJhbGciOiJIUzI1NiJ9.x.y");

        await storage.EliminarAsync(Key, default);

        Assert.Equal("Bearer eyJhbGciOiJIUzI1NiJ9.x.y", Assert.Single(handler.Peticiones).Encabezados["Authorization"]);
    }

    [Fact]
    public async Task Url_de_descarga_firmada_por_5_minutos_con_nombre_de_descarga()
    {
        var (storage, handler) = Crear("sb_secret_abc123",
            _ => Json(new { signedURL = $"/object/sign/upractice-archivos/{Key}?token=tok123" }));

        var url = await storage.ObtenerUrlDescargaAsync(Key, LlavesArchivo.VigenciaDescarga, "CV Ana López.pdf", default);

        var p = Assert.Single(handler.Peticiones);
        Assert.Equal($"{Url}/storage/v1/object/sign/upractice-archivos/{Key}", p.Url);
        Assert.Equal(300, JsonDocument.Parse(p.Cuerpo!).RootElement.GetProperty("expiresIn").GetInt32());
        Assert.Equal($"{Url}/storage/v1/object/sign/upractice-archivos/{Key}?token=tok123&download=CV%20Ana%20L%C3%B3pez.pdf", url.Url);
        Assert.Equal(reloj.GetUtcNow().AddMinutes(5).UtcDateTime, url.ExpiraEn);
    }

    [Fact]
    public async Task Eliminar_un_objeto_inexistente_no_falla()
    {
        var (storage, handler) = Crear("sb_secret_abc123", _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        await storage.EliminarAsync(Key, default);

        Assert.Equal(HttpMethod.Delete, Assert.Single(handler.Peticiones).Metodo);
    }

    [Fact]
    public async Task Un_error_de_supabase_se_propaga_con_el_detalle()
    {
        var (storage, _) = Crear("sb_secret_abc123",
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"error\":\"Bucket not found\"}") });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.SubirAsync(Key, new MemoryStream([1]), "application/pdf", default));
        Assert.Contains("Bucket not found", error.Message);
    }

    [Fact]
    public async Task Rechaza_llaves_peligrosas_antes_de_llamar_a_supabase()
    {
        var (storage, handler) = Crear("sb_secret_abc123");

        await Assert.ThrowsAsync<ArgumentException>(() => storage.EliminarAsync("../otro-bucket/x.pdf", default));
        Assert.Empty(handler.Peticiones);
    }

    private (SupabaseStorage, HandlerFalso) Crear(string clave, Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var opciones = OpcionesAlmacenamiento.Desde(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Proveedor"] = "Supabase",
            ["Storage:SupabaseUrl"] = Url + "/",
            ["Storage:SupabaseClave"] = clave,
        }).Build());
        var handler = new HandlerFalso(responder ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var http = new HttpClient(handler);
        SupabaseStorage.ConfigurarCliente(http, opciones);
        return (new SupabaseStorage(http, opciones, reloj), handler);
    }

    private static HttpResponseMessage Json(object valor) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(valor), Encoding.UTF8, "application/json") };

    private sealed record Peticion(HttpMethod Metodo, string Url, Dictionary<string, string> Encabezados, string? ContentType, string? Cuerpo);

    private sealed class HandlerFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Peticion> Peticiones { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Peticiones.Add(new Peticion(
                request.Method,
                request.RequestUri!.AbsoluteUri,
                request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
                request.Content?.Headers.ContentType?.MediaType,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(ct)));
            return responder(request);
        }
    }
}
