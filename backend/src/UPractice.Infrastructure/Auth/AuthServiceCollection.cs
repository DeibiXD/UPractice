using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Auth;

internal static class AuthServiceCollection
{
    // T-03: registrar generador JWE e ICurrentUser.
    internal static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IHasherPassword, HasherPassword>();
        return services;
    }
}
