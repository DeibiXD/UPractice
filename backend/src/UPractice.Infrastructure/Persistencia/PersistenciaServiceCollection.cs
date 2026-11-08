using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UPractice.Infrastructure.Persistencia;

internal static class PersistenciaServiceCollection
{
    // T-02: registrar AppDbContext (Npgsql), IAppDbContext y Seeder.
    internal static IServiceCollection AddPersistencia(this IServiceCollection services, IConfiguration configuration)
        => services;
}
