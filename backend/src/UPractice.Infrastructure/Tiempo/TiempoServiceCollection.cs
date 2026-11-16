using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Tiempo;

internal static class TiempoServiceCollection
{
    internal static IServiceCollection AddTiempo(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IFechaHonduras, FechaHonduras>();
        return services;
    }
}
