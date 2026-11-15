using FluentValidation;
using UPractice.Api.Errores;
using UPractice.Domain.Excepciones;

namespace UPractice.Api.Middleware;

/// <summary>
/// Middleware global (lineamiento de la Fase 2): traduce las excepciones a ProblemDetails (S5.3, S5.4).
/// Ningún controlador ni handler arma respuestas de error a mano; solo lanzan la excepción correcta.
/// </summary>
public sealed class ManejadorErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext http)
    {
        try
        {
            await siguiente(http);
        }
        catch (Exception ex) when (!http.Response.HasStarted)
        {
            var problema = ex switch
            {
                ValidationException v => Problemas.Validacion(http, AgruparErrores(v)),
                ReglaNegocioException r => Problemas.ReglaNegocio(http, r.Code, r.Message),
                ConflictoException c => Problemas.Conflicto(http, c.Code, c.Message),
                NoEncontradoException n => Problemas.NoEncontrado(http, n.Message),
                NoAutenticadoException a => Problemas.NoAutenticado(http, a.Message),
                _ => null,
            };

            if (problema is null)
            {
                // Error no controlado: se registra completo, pero al cliente nunca se le envía el stack trace.
                logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", http.Request.Method, http.Request.Path);
                problema = Problemas.ErrorInterno(http);
            }

            await Problemas.EscribirAsync(http, problema);
        }
    }

    /// <summary>Agrupa los errores por campo. Los nombres ya llegan en camelCase (PropertyNameResolver).</summary>
    internal static Dictionary<string, string[]> AgruparErrores(ValidationException ex) =>
        ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
}
