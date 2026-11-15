using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace UPractice.Api.Errores;

/// <summary>
/// Arma las respuestas ProblemDetails (RFC 9457) con el formato exacto de S5.3 del diseño.
/// Lo usan el middleware de errores, la respuesta de ModelState inválido y el filtro de contraseña temporal,
/// para que el extractor de errores del frontend reciba siempre la misma forma.
/// </summary>
public static class Problemas
{
    private const string BaseTipo = "https://upractice.app/errores/";

    public static ProblemDetails Validacion(HttpContext http, IDictionary<string, string[]> errores) =>
        Crear(http, StatusCodes.Status400BadRequest, "validacion", "Uno o más campos no son válidos.",
            extensiones: new() { ["errors"] = errores });

    public static ProblemDetails ReglaNegocio(HttpContext http, string code, string detalle) =>
        Crear(http, StatusCodes.Status422UnprocessableEntity, "regla-negocio", "No es posible completar la operación.", detalle,
            new() { ["code"] = code });

    public static ProblemDetails Conflicto(HttpContext http, string code, string detalle) =>
        Crear(http, StatusCodes.Status409Conflict, "conflicto", "El recurso ya existe.", detalle, new() { ["code"] = code });

    public static ProblemDetails NoEncontrado(HttpContext http, string detalle) =>
        Crear(http, StatusCodes.Status404NotFound, "no-encontrado", "No se encontró el recurso.", detalle);

    public static ProblemDetails NoAutenticado(HttpContext http, string detalle) =>
        Crear(http, StatusCodes.Status401Unauthorized, "no-autenticado", "No autenticado.", detalle);

    public static ProblemDetails Prohibido(HttpContext http, string detalle, string? code = null) =>
        Crear(http, StatusCodes.Status403Forbidden, "prohibido", "No tiene permiso para esta operación.", detalle,
            code is null ? null : new() { ["code"] = code });

    public static ProblemDetails ErrorInterno(HttpContext http) =>
        Crear(http, StatusCodes.Status500InternalServerError, "interno", "Ocurrió un error inesperado.",
            "Intente de nuevo. Si el problema continúa, comuníquese con soporte e indique el traceId.");

    private static ProblemDetails Crear(HttpContext http, int status, string tipo, string titulo, string? detalle = null,
        Dictionary<string, object?>? extensiones = null)
    {
        var problema = new ProblemDetails
        {
            Type = BaseTipo + tipo,
            Title = titulo,
            Status = status,
            Detail = detalle,
            Instance = http.Request.Path,
        };
        foreach (var (clave, valor) in extensiones ?? [])
            problema.Extensions[clave] = valor;
        problema.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;
        return problema;
    }

    /// <summary>Escribe el problema como application/problem+json.</summary>
    public static Task EscribirAsync(HttpContext http, ProblemDetails problema)
    {
        http.Response.StatusCode = problema.Status ?? StatusCodes.Status500InternalServerError;
        return http.Response.WriteAsJsonAsync(problema, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
