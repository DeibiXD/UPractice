using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Almacenamiento;

/// <summary>
/// Proveedor de producción (S3.8 / plan 4.7): bucket PRIVADO de Supabase Storage vía su API REST.
/// <list type="bullet">
/// <item>Subir: <c>POST /storage/v1/object/{bucket}/{key}</c> (con <c>x-upsert</c> para reemplazar).</item>
/// <item>URL firmada: <c>POST /storage/v1/object/sign/{bucket}/{key}</c> con <c>{ "expiresIn": segundos }</c>.</item>
/// <item>Eliminar: <c>DELETE /storage/v1/object/{bucket}/{key}</c>.</item>
/// </list>
/// El <see cref="HttpClient"/> ya trae la dirección base y las credenciales (ver <see cref="ConfigurarCliente"/>).
/// </summary>
public sealed class SupabaseStorage(HttpClient http, OpcionesAlmacenamiento opciones, TimeProvider reloj) : IAlmacenamientoArchivos
{
    public async Task SubirAsync(string key, Stream contenido, string contentType, CancellationToken ct)
    {
        using var cuerpo = new StreamContent(contenido);
        cuerpo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var peticion = new HttpRequestMessage(HttpMethod.Post, RutaObjeto(key)) { Content = cuerpo };
        peticion.Headers.Add("x-upsert", "true");

        using var respuesta = await http.SendAsync(peticion, ct);
        await AsegurarExitoAsync(respuesta, "subir", key, ct);
    }

    public async Task<UrlDescarga> ObtenerUrlDescargaAsync(string key, TimeSpan vigencia, string nombreDescarga, CancellationToken ct)
    {
        var segundos = (int)Math.Ceiling(vigencia.TotalSeconds);
        var expira = reloj.GetUtcNow().AddSeconds(segundos).UtcDateTime;

        using var respuesta = await http.PostAsJsonAsync($"storage/v1/object/sign/{opciones.Bucket}/{Ruta(key)}",
            new { expiresIn = segundos }, ct);
        await AsegurarExitoAsync(respuesta, "firmar", key, ct);

        var firmada = await respuesta.Content.ReadFromJsonAsync<RespuestaFirma>(ct);
        if (string.IsNullOrWhiteSpace(firmada?.SignedUrl))
            throw new InvalidOperationException($"Supabase Storage no devolvió la URL firmada de '{key}'.");

        // signedURL viene relativa a /storage/v1 (ej. "/object/sign/bucket/key?token=…").
        var url = $"{opciones.SupabaseUrl}/storage/v1{firmada.SignedUrl}";
        if (!string.IsNullOrWhiteSpace(nombreDescarga))
            url += (url.Contains('?') ? "&" : "?") + "download=" + Uri.EscapeDataString(nombreDescarga);

        return new UrlDescarga(url, expira);
    }

    public async Task EliminarAsync(string key, CancellationToken ct)
    {
        using var respuesta = await http.DeleteAsync(RutaObjeto(key), ct);
        if (respuesta.StatusCode is HttpStatusCode.NotFound) return; // ya no existía: no es un error
        await AsegurarExitoAsync(respuesta, "eliminar", key, ct);
    }

    /// <summary>Dirección base y credenciales. La clave nueva (sb_secret_…) va solo en <c>apikey</c>; la heredada (JWT service_role) también como Bearer.</summary>
    public static void ConfigurarCliente(HttpClient http, OpcionesAlmacenamiento opciones)
    {
        http.BaseAddress = new Uri(opciones.SupabaseUrl + "/");
        http.Timeout = TimeSpan.FromSeconds(30);
        http.DefaultRequestHeaders.Add("apikey", opciones.SupabaseClave);
        if (opciones.SupabaseClave.StartsWith("eyJ", StringComparison.Ordinal))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opciones.SupabaseClave);
    }

    private string RutaObjeto(string key) => $"storage/v1/object/{opciones.Bucket}/{Ruta(key)}";

    private static string Ruta(string key) =>
        string.Join('/', LlaveArchivo.Validar(key).Split('/').Select(Uri.EscapeDataString));

    private static async Task AsegurarExitoAsync(HttpResponseMessage respuesta, string accion, string key, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode) return;
        var detalle = await respuesta.Content.ReadAsStringAsync(ct);
        // El middleware lo convierte en 500 sin exponer el detalle; el mensaje queda en los logs de Render.
        throw new InvalidOperationException(
            $"Supabase Storage no pudo {accion} '{key}': {(int)respuesta.StatusCode} {detalle}");
    }

    private sealed record RespuestaFirma([property: JsonPropertyName("signedURL")] string? SignedUrl);
}
