using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UPractice.Infrastructure.Almacenamiento;

internal static class AlmacenamientoServiceCollection
{
    // T-08: registrar IAlmacenamientoArchivos (LocalStorage o SupabaseStorage según configuración).
    internal static IServiceCollection AddAlmacenamiento(this IServiceCollection services, IConfiguration configuration)
        => services;
}
