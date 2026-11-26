using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using UPractice.Application.Abstracciones;
using UPractice.Infrastructure.Persistencia;

namespace UPractice.Tests.Infraestructura;

/// <summary>
/// Levanta la API completa contra una base PostgreSQL real con la migración y el seed de demostración
/// (2 empresas, 2 universidades, estudiantes, vacantes y una postulación).
///
/// Base de datos:
/// - Si existe la variable PRUEBAS_POSTGRES (cadena de conexión a un servidor), crea ahí una base nueva
///   con nombre aleatorio y la borra al terminar. Así corren en Docker o en CI con un PostgreSQL existente.
/// - Si no, levanta un contenedor efímero con Testcontainers (requiere Docker).
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string PasswordDemo = "Demo-Pruebas-2026";
    public const string PasswordSuperAdmin = "Admin-Pruebas-2026";

    private PostgreSqlContainer? contenedor;
    private string cadenaConexion = null!;
    private readonly string carpetaArchivos = Path.Combine(Path.GetTempPath(), "upractice-pruebas-" + Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync()
    {
        var servidor = Environment.GetEnvironmentVariable("PRUEBAS_POSTGRES");
        if (string.IsNullOrWhiteSpace(servidor))
        {
            contenedor = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await contenedor.StartAsync();
            servidor = contenedor.GetConnectionString();
        }
        cadenaConexion = new NpgsqlConnectionStringBuilder(servidor) { Database = $"upractice_pruebas_{Guid.NewGuid():N}" }.ConnectionString;

        // Variables de entorno: tienen prioridad sobre appsettings.json, que trae estos valores vacíos.
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", cadenaConexion);
        Environment.SetEnvironmentVariable("Db__AplicarMigraciones", "true");
        Environment.SetEnvironmentVariable("Seed__Demo", "true");
        Environment.SetEnvironmentVariable("Seed__DemoPassword", PasswordDemo);
        Environment.SetEnvironmentVariable("Seed__SuperAdminPassword", PasswordSuperAdmin);
        Environment.SetEnvironmentVariable("Jwt__ClaveFirma", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        Environment.SetEnvironmentVariable("Jwt__ClaveCifrado", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        // T-08: proveedor local en una carpeta temporal (se borra al terminar).
        Environment.SetEnvironmentVariable("Storage__Proveedor", "Local");
        Environment.SetEnvironmentVariable("Storage__RutaLocal", carpetaArchivos);

        // Fuerza el arranque del host: aplica la migración y el seed.
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Pruebas");

        // Controlador que solo existe en las pruebas (ver PruebasController).
        builder.ConfigureServices(s => s.AddControllers().AddApplicationPart(typeof(ApiFixture).Assembly));
    }

    /// <summary>AppDbContext con un usuario fijo, para probar los filtros globales sin pasar por HTTP.</summary>
    public AppDbContext CrearContexto(ICurrentUser usuario)
    {
        var opciones = Services.GetRequiredService<DbContextOptions<AppDbContext>>();
        return new AppDbContext(opciones, usuario);
    }

    /// <summary>Contexto sin usuario: los filtros no devuelven filas. Para preparar datos usar IgnoreQueryFilters().</summary>
    public AppDbContext CrearContextoSinUsuario() => CrearContexto(new UsuarioDePrueba());

    public T Servicio<T>() where T : notnull => Services.GetRequiredService<T>();

    public async Task<HttpClient> ClienteAutenticadoAsync(string correo, string password)
    {
        var cliente = CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo, password });
        respuesta.EnsureSuccessStatusCode();
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return cliente;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var db = CrearContextoSinUsuario())
            await db.Database.EnsureDeletedAsync();
        await base.DisposeAsync();
        if (contenedor is not null) await contenedor.DisposeAsync();
        if (Directory.Exists(carpetaArchivos)) Directory.Delete(carpetaArchivos, recursive: true);
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionApi : ICollectionFixture<ApiFixture>
{
    public const string Nombre = "API con base de datos";
}
