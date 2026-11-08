using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UPractice.Infrastructure.Auth;

internal static class AuthServiceCollection
{
    // T-03: registrar generador JWE, ICurrentUser y PasswordHasher.
    internal static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
        => services;
}
