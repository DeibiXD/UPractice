using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UPractice.Application.Abstracciones;
using UPractice.Infrastructure.Persistencia.Seed;

namespace UPractice.Infrastructure.Persistencia;

internal static class PersistenciaServiceCollection
{
    internal static IServiceCollection AddPersistencia(this IServiceCollection services, IConfiguration configuration)
    {
        var cadena = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Postgres (variable ConnectionStrings__Postgres).");

        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cadena));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<Seeder>();
        return services;
    }
}
