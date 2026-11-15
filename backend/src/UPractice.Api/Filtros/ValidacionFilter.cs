using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UPractice.Api.Filtros;

/// <summary>
/// Antes de ejecutar cada acción, busca un IValidator&lt;T&gt; para cada argumento y lo ejecuta.
/// Si falla, lanza ValidationException y el middleware responde 400 con los errores por campo.
/// Así ninguna HU tiene que llamar al validador a mano: basta con crear la clase *Validator.
/// </summary>
public sealed class ValidacionFilter(IServiceProvider servicios) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argumento in context.ActionArguments.Values)
        {
            if (argumento is null) continue;

            var tipoValidador = typeof(IValidator<>).MakeGenericType(argumento.GetType());
            if (servicios.GetService(tipoValidador) is not IValidator validador) continue;

            var resultado = await validador.ValidateAsync(new ValidationContext<object>(argumento), context.HttpContext.RequestAborted);
            if (!resultado.IsValid)
                throw new ValidationException(resultado.Errors);
        }

        await next();
    }
}
