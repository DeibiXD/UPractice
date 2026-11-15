using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;

namespace UPractice.Infrastructure.Auth;

/// <summary>
/// Token firmado (HS256) y además cifrado (JWE: A256KW + A256CBC-HS512), D-15.
/// El lineamiento pide que el EmpresaId viaje encriptado: un JWT solo firmado se puede leer en jwt.io.
/// </summary>
internal sealed class GeneradorJwt(OpcionesJwt opciones) : IGeneradorToken
{
    public const string ClaimEmpresa = "empresa_id";
    public const string ClaimUniversidad = "universidad_id";
    public const string ClaimPasswordTemporal = "pwd_temp";

    private static readonly JsonWebTokenHandler Handler = new();

    public TokenEmitido Generar(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(opciones.MinutosExpiracion);
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = usuario.Correo,
            [JwtRegisteredClaimNames.Name] = usuario.NombreCompleto,
            ["role"] = usuario.Rol.ToString(),
        };
        if (usuario.EmpresaId is { } empresaId) claims[ClaimEmpresa] = empresaId.ToString();
        if (usuario.UniversidadId is { } universidadId) claims[ClaimUniversidad] = universidadId.ToString();
        if (usuario.DebeCambiarPassword) claims[ClaimPasswordTemporal] = true;

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = opciones.Issuer,
            Audience = opciones.Audience,
            Claims = claims,
            Expires = expira,
            SigningCredentials = new SigningCredentials(opciones.ClaveFirma, SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(
                opciones.ClaveCifrado, SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512),
        });
        return new TokenEmitido(token, expira);
    }
}
