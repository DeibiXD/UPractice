using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UPractice.Infrastructure.Persistencia;

/// <summary>
/// Solo para `dotnet ef migrations add`: crea el contexto sin levantar la API.
/// La cadena no se usa para conectarse al generar migraciones.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=upractice_diseno")
            .Options);
}
