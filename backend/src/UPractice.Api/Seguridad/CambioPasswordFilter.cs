using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UPractice.Api.Errores;
using UPractice.Domain.Excepciones;

namespace UPractice.Api.Seguridad;

/// <summary>
/// Mientras el token traiga "pwd_temp", cualquier acción sin [PermitirPasswordTemporal]
/// responde 403 CAMBIO_PASSWORD_REQUERIDO (D-17). El interceptor de Axios redirige a /cambiar-password.
/// </summary>
public sealed class CambioPasswordFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var usuario = context.HttpContext.User;
        if (usuario.Identity?.IsAuthenticated != true || usuario.FindFirst("pwd_temp")?.Value != "true")
            return;
        if (context.ActionDescriptor.EndpointMetadata.OfType<PermitirPasswordTemporalAttribute>().Any())
            return;

        var problema = Problemas.Prohibido(context.HttpContext,
            "Debe cambiar su contraseña temporal antes de continuar.", CodigosError.CambioPasswordRequerido);
        context.Result = new ObjectResult(problema) { StatusCode = problema.Status, ContentTypes = { "application/problem+json" } };
    }
}
