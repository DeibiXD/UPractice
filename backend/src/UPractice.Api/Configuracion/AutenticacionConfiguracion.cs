using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using UPractice.Api.Errores;
using UPractice.Api.Seguridad;
using UPractice.Application.Abstracciones;
using UPractice.Infrastructure.Auth;

namespace UPractice.Api.Configuracion;

public static class AutenticacionConfiguracion
{
    /// <summary>
    /// JwtBearer que descifra (TokenDecryptionKey) y valida la firma del token JWE (S5.2).
    /// Toda ruta exige token salvo las marcadas con [AllowAnonymous] (política de respaldo).
    /// </summary>
    public static IServiceCollection AddAutenticacionUPractice(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<OpcionesJwt>((o, jwt) =>
            {
                o.MapInboundClaims = false; // conserva "sub", "role", "empresa_id"… tal cual
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.ClaveFirma,
                    TokenDecryptionKey = jwt.ClaveCifrado,
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
                o.Events = new JwtBearerEvents
                {
                    // 401 y 403 con el mismo formato ProblemDetails que el resto de errores.
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        var detalle = ctx.AuthenticateFailure is SecurityTokenExpiredException
                            ? "La sesión expiró. Inicie sesión de nuevo."
                            : "Debe iniciar sesión.";
                        await Problemas.EscribirAsync(ctx.HttpContext, Problemas.NoAutenticado(ctx.HttpContext, detalle));
                    },
                    OnForbidden = ctx => Problemas.EscribirAsync(ctx.HttpContext,
                        Problemas.Prohibido(ctx.HttpContext, "Su rol no tiene permiso para esta operación.")),
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
