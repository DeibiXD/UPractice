using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Almacenamiento;

/// <summary>
/// Proveedor de desarrollo y Docker (S3.8): guarda en una carpeta (<c>Storage:RutaLocal</c>) y genera URL hacia la
/// propia API, <c>GET /api/v1/archivos/local?key=…&amp;exp=…&amp;nombre=…&amp;sig=…</c>, firmadas con HMAC-SHA256.
/// Así el frontend descarga igual que con Supabase: abre la URL y, al vencer, deja de funcionar.
/// </summary>
public sealed class LocalStorage(OpcionesAlmacenamiento opciones, TimeProvider reloj) : IAlmacenamientoArchivos
{
    public const string RutaDescarga = "/api/v1/archivos/local";

    public async Task SubirAsync(string key, Stream contenido, string contentType, CancellationToken ct)
    {
        var destino = RutaFisica(LlaveArchivo.Validar(key));
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        // Se escribe en un temporal y luego se mueve: nunca queda un PDF a medias en la llave final.
        var temporal = destino + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var archivo = new FileStream(temporal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await contenido.CopyToAsync(archivo, ct);
            File.Move(temporal, destino, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporal)) File.Delete(temporal);
        }
    }

    public Task<UrlDescarga> ObtenerUrlDescargaAsync(string key, TimeSpan vigencia, string nombreDescarga, CancellationToken ct)
    {
        LlaveArchivo.Validar(key);
        var expira = reloj.GetUtcNow().Add(vigencia);
        var exp = expira.ToUnixTimeSeconds();
        var nombre = string.IsNullOrWhiteSpace(nombreDescarga) ? Path.GetFileName(key) : nombreDescarga;

        var url = $"{opciones.UrlBaseLocal}{RutaDescarga}" +
                  $"?key={Uri.EscapeDataString(key)}" +
                  $"&exp={exp.ToString(CultureInfo.InvariantCulture)}" +
                  $"&nombre={Uri.EscapeDataString(nombre)}" +
                  $"&sig={Firmar(key, exp, nombre)}";
        return Task.FromResult(new UrlDescarga(url, DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime));
    }

    public Task EliminarAsync(string key, CancellationToken ct)
    {
        var ruta = RutaFisica(LlaveArchivo.Validar(key));
        if (File.Exists(ruta)) File.Delete(ruta);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lo usa el endpoint de descarga local. Devuelve null si la firma no coincide, la URL venció o el archivo no existe;
    /// el endpoint responde 404 en todos los casos para no dar pistas.
    /// </summary>
    public Stream? AbrirSiFirmaValida(string? key, long exp, string? nombre, string? sig)
    {
        if (!LlaveArchivo.EsValida(key) || nombre is null || string.IsNullOrEmpty(sig)) return null;
        if (reloj.GetUtcNow().ToUnixTimeSeconds() > exp) return null;

        var esperada = Encoding.ASCII.GetBytes(Firmar(key!, exp, nombre));
        var recibida = Encoding.ASCII.GetBytes(sig);
        if (!CryptographicOperations.FixedTimeEquals(esperada, recibida)) return null;

        var ruta = RutaFisica(key!);
        return File.Exists(ruta)
            ? new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null;
    }

    private string Firmar(string key, long exp, string nombre)
    {
        var datos = Encoding.UTF8.GetBytes($"{key}\n{exp.ToString(CultureInfo.InvariantCulture)}\n{nombre}");
        // Base64 URL-safe sin relleno: no necesita escaparse en la query string.
        return Convert.ToBase64String(HMACSHA256.HashData(opciones.ClaveFirmaLocal, datos))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private string RutaFisica(string key)
    {
        var raiz = Path.GetFullPath(opciones.RutaLocal) + Path.DirectorySeparatorChar;
        var ruta = Path.GetFullPath(Path.Combine(raiz, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!ruta.StartsWith(raiz, StringComparison.Ordinal))
            throw new ArgumentException($"La llave de archivo '{key}' sale de la carpeta de almacenamiento.", nameof(key));
        return ruta;
    }
}
