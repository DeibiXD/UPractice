using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace UPractice.Infrastructure.Auth;

/// <summary>
/// Configuración del token (S5.2). Las claves vienen en Base64 desde variables de entorno o user-secrets:
/// Jwt__ClaveFirma (mínimo 32 bytes) y Jwt__ClaveCifrado (exactamente 32 bytes). Nunca van en el repositorio.
/// </summary>
public sealed class OpcionesJwt
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int MinutosExpiracion { get; init; }
    public required SymmetricSecurityKey ClaveFirma { get; init; }
    public required SymmetricSecurityKey ClaveCifrado { get; init; }
    /// <summary>true si se generaron claves al azar porque faltaban (solo desarrollo).</summary>
    public bool ClavesTemporales { get; init; }

    public static OpcionesJwt Desde(IConfiguration configuration)
    {
        var seccion = configuration.GetSection("Jwt");
        var firma = seccion["ClaveFirma"];
        var cifrado = seccion["ClaveCifrado"];
        var temporales = false;

        if (string.IsNullOrWhiteSpace(firma) || string.IsNullOrWhiteSpace(cifrado))
        {
            // Jwt:GenerarClavesTemporales solo está en appsettings.Development.json: en producción faltar
            // una clave detiene la API, en desarrollo se generan al azar (los tokens mueren al reiniciar).
            if (!seccion.GetValue<bool>("GenerarClavesTemporales"))
                throw new InvalidOperationException("Faltan Jwt:ClaveFirma y/o Jwt:ClaveCifrado (variables Jwt__ClaveFirma y Jwt__ClaveCifrado).");
            firma = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            cifrado = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            temporales = true;
        }

        var bytesFirma = Leer(firma, "Jwt:ClaveFirma");
        var bytesCifrado = Leer(cifrado, "Jwt:ClaveCifrado");
        if (bytesFirma.Length < 32)
            throw new InvalidOperationException("Jwt:ClaveFirma debe tener al menos 32 bytes.");
        if (bytesCifrado.Length != 32)
            throw new InvalidOperationException("Jwt:ClaveCifrado debe tener exactamente 32 bytes (A256KW).");
        if (bytesFirma.AsSpan().SequenceEqual(bytesCifrado))
            throw new InvalidOperationException("Jwt:ClaveFirma y Jwt:ClaveCifrado deben ser distintas.");

        return new OpcionesJwt
        {
            Issuer = seccion["Issuer"] ?? "upractice-api",
            Audience = seccion["Audience"] ?? "upractice-web",
            MinutosExpiracion = seccion.GetValue("MinutosExpiracion", 60),
            ClaveFirma = new SymmetricSecurityKey(bytesFirma),
            ClaveCifrado = new SymmetricSecurityKey(bytesCifrado),
            ClavesTemporales = temporales,
        };
    }

    private static byte[] Leer(string base64, string nombre)
    {
        try { return Convert.FromBase64String(base64); }
        catch (FormatException) { throw new InvalidOperationException($"{nombre} debe estar en Base64."); }
    }
}
