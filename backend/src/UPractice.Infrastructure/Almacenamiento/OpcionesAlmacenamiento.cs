using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace UPractice.Infrastructure.Almacenamiento;

public enum ProveedorAlmacenamiento { Local, Supabase }

/// <summary>
/// Sección <c>Storage</c> (S3.8). En variables de entorno: <c>Storage__Proveedor</c>, <c>Storage__SupabaseUrl</c>, etc.
/// La clave de Supabase (service role / secret) solo vive en Render; nunca en el repositorio ni en el frontend.
/// </summary>
public sealed class OpcionesAlmacenamiento
{
    public required ProveedorAlmacenamiento Proveedor { get; init; }

    /// <summary>Carpeta raíz del proveedor local (en Docker: <c>/app/storage</c>, montada como volumen).</summary>
    public required string RutaLocal { get; init; }

    /// <summary>URL pública de la API, para armar los enlaces del proveedor local (ej. <c>http://localhost:8080</c> en Docker).</summary>
    public required string UrlBaseLocal { get; init; }

    /// <summary>Clave HMAC de las URL locales. Si falta se genera al azar (los enlaces mueren al reiniciar: solo desarrollo).</summary>
    public required byte[] ClaveFirmaLocal { get; init; }

    public string SupabaseUrl { get; init; } = "";
    public string SupabaseClave { get; init; } = "";
    public required string Bucket { get; init; }

    public static OpcionesAlmacenamiento Desde(IConfiguration configuration)
    {
        var s = configuration.GetSection("Storage");

        var textoProveedor = s["Proveedor"];
        var proveedor = ProveedorAlmacenamiento.Local;
        if (!string.IsNullOrWhiteSpace(textoProveedor) && !Enum.TryParse(textoProveedor, ignoreCase: true, out proveedor))
            throw new InvalidOperationException($"Storage:Proveedor '{textoProveedor}' no es válido. Use Local o Supabase.");

        var clave = s["ClaveFirmaLocal"];
        byte[] bytesClave;
        if (string.IsNullOrWhiteSpace(clave))
            bytesClave = RandomNumberGenerator.GetBytes(32);
        else
        {
            try { bytesClave = Convert.FromBase64String(clave); }
            catch (FormatException) { throw new InvalidOperationException("Storage:ClaveFirmaLocal debe estar en Base64."); }
            if (bytesClave.Length < 32)
                throw new InvalidOperationException("Storage:ClaveFirmaLocal debe tener al menos 32 bytes.");
        }

        var opciones = new OpcionesAlmacenamiento
        {
            Proveedor = proveedor,
            RutaLocal = Path.GetFullPath(string.IsNullOrWhiteSpace(s["RutaLocal"]) ? "storage" : s["RutaLocal"]!),
            UrlBaseLocal = (s["UrlBaseLocal"] ?? "http://localhost:5294").TrimEnd('/'),
            ClaveFirmaLocal = bytesClave,
            SupabaseUrl = (s["SupabaseUrl"] ?? "").TrimEnd('/'),
            SupabaseClave = s["SupabaseClave"] ?? "",
            Bucket = string.IsNullOrWhiteSpace(s["Bucket"]) ? "upractice-archivos" : s["Bucket"]!,
        };

        if (proveedor == ProveedorAlmacenamiento.Supabase &&
            (string.IsNullOrWhiteSpace(opciones.SupabaseUrl) || string.IsNullOrWhiteSpace(opciones.SupabaseClave)))
            throw new InvalidOperationException(
                "Con Storage:Proveedor = Supabase faltan Storage:SupabaseUrl y/o Storage:SupabaseClave (variables Storage__SupabaseUrl y Storage__SupabaseClave).");

        return opciones;
    }
}
