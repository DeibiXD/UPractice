using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UPractice.Application.Abstracciones;

namespace UPractice.Infrastructure.Almacenamiento;

internal static class AlmacenamientoServiceCollection
{
    // T-08: IAlmacenamientoArchivos según Storage:Proveedor (Local por defecto; Supabase en Render).
    internal static IServiceCollection AddAlmacenamiento(this IServiceCollection services, IConfiguration configuration)
    {
        var opciones = OpcionesAlmacenamiento.Desde(configuration);
        services.AddSingleton(opciones);
        services.TryAddSingleton(TimeProvider.System);

        if (opciones.Proveedor == ProveedorAlmacenamiento.Supabase)
        {
            // Un solo HttpClient para toda la vida de la API (evita agotar sockets).
            services.AddSingleton<IAlmacenamientoArchivos>(sp =>
            {
                var http = new HttpClient();
                SupabaseStorage.ConfigurarCliente(http, opciones);
                return new SupabaseStorage(http, opciones, sp.GetRequiredService<TimeProvider>());
            });
        }
        else
        {
            // La clase concreta también se registra: la usa el endpoint de descarga local.
            services.AddSingleton<LocalStorage>();
            services.AddSingleton<IAlmacenamientoArchivos>(sp => sp.GetRequiredService<LocalStorage>());
        }

        return services;
    }
}
