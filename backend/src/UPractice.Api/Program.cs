using UPractice.Application;
using UPractice.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { estado = "ok" })); // T-07 lo usa en Render

app.Run();

public partial class Program; // necesario para WebApplicationFactory en las pruebas
