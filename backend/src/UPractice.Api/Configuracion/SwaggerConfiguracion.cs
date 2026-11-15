using Microsoft.OpenApi;

namespace UPractice.Api.Configuracion;

public static class SwaggerConfiguracion
{
    /// <summary>OpenAPI con esquema Bearer: el botón Authorize de Swagger UI agrega el token a cada petición.</summary>
    public static IServiceCollection AddSwaggerUPractice(this IServiceCollection services) =>
        services.AddOpenApi(o => o.AddDocumentTransformer((documento, _, _) =>
        {
            documento.Info.Title = "UPractice API";
            documento.Info.Description = "Contrato vivo de la API (S5 de docs/fase-1/UPractice-Fase1.md). "
                + "Para probar: POST /api/v1/auth/login, copiar accessToken y pegarlo en Authorize.";
            documento.Components ??= new OpenApiComponents();
            documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWE",
                Description = "Pegue solo el accessToken (sin la palabra Bearer).",
            };
            documento.Security ??= [];
            documento.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", documento)] = [] });
            return Task.CompletedTask;
        }));

    public static WebApplication UseSwaggerUPractice(this WebApplication app)
    {
        app.MapOpenApi().AllowAnonymous();               // /openapi/v1.json
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/openapi/v1.json", "UPractice API v1");
            o.RoutePrefix = "swagger";
            o.DocumentTitle = "UPractice API";
        });
        return app;
    }
}
