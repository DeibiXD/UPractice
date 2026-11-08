using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UPractice.Infrastructure.Almacenamiento;
using UPractice.Infrastructure.Auth;
using UPractice.Infrastructure.Persistencia;

namespace UPractice.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Cada tarea técnica es dueña de SU método en SU archivo para no chocar en este:
    /// T-02 → AddPersistencia · T-03 → AddAuth · T-08 → AddAlmacenamiento.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddPersistencia(configuration)
            .AddAuth(configuration)
            .AddAlmacenamiento(configuration);
}
