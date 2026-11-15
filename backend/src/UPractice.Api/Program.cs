using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Api.Configuracion;
using UPractice.Api.Errores;
using UPractice.Api.Filtros;
using UPractice.Api.Middleware;
using UPractice.Api.Seguridad;
using UPractice.Application;
using UPractice.Infrastructure;
using UPractice.Infrastructure.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutenticacionUPractice();
builder.Services.AddSwaggerUPractice();

builder.Services
    .AddControllers(o =>
    {
        o.Filters.Add<CambioPasswordFilter>();   // T-03: 403 CAMBIO_PASSWORD_REQUERIDO (D-17)
        o.Filters.Add<ValidacionFilter>();       // T-04: FluentValidation antes de cada acción
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        // JSON mal formado o tipos incorrectos: mismo formato 400 que FluentValidation.
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errores = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => UPractice.Application.DependencyInjection.CamelCase(e.Key.TrimStart('$', '.')) is { Length: > 0 } k ? k : "general",
                    e => e.Value!.Errors.Select(_ => "El valor enviado no tiene el formato correcto.").Distinct().ToArray());
            var problema = Problemas.Validacion(ctx.HttpContext, errores);
            return new ObjectResult(problema) { StatusCode = problema.Status, ContentTypes = { "application/problem+json" } };
        };
    });

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// T-02: migraciones + seed al arrancar si Db:AplicarMigraciones = true (S2.2).
await app.Services.InicializarBaseDatosAsync(app.Configuration);

app.UseMiddleware<ManejadorErroresMiddleware>();   // T-04: primero, para atrapar todo lo que venga después
app.UseCors();
app.UseSwaggerUPractice();                          // antes de la autorización: la página de Swagger es pública
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { estado = "ok" })).AllowAnonymous(); // Render lo consulta sin token

app.Run();

public partial class Program; // necesario para WebApplicationFactory en las pruebas
