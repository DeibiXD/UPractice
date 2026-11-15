using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Auth;

internal static class AuthServiceCollection
{
    // ICurrentUser lo registra la Api (lee el HttpContext); aquí van el hash, las claves y el generador del token.
    internal static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(OpcionesJwt.Desde(configuration));
        services.AddSingleton<IHasherPassword, HasherPassword>();
        services.AddSingleton<IGeneradorToken, GeneradorJwt>();
        return services;
    }
}
