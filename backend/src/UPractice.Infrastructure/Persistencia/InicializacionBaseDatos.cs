using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UPractice.Infrastructure.Persistencia.Seed;

namespace UPractice.Infrastructure.Persistencia;

public static class InicializacionBaseDatos
{
    /// <summary>
    /// Si Db:AplicarMigraciones = true, aplica las migraciones pendientes y corre el seed (§2.2).
    /// En Render se deja en true para que cada despliegue actualice la BD sola.
    /// </summary>
    public static async Task InicializarBaseDatosAsync(this IServiceProvider servicios, IConfiguration configuration, CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("Db:AplicarMigraciones")) return;

        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
        await scope.ServiceProvider.GetRequiredService<Seeder>().EjecutarAsync(ct);
    }
}
