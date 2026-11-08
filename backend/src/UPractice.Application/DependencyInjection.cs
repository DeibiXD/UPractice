using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace UPractice.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra automáticamente todos los validadores y todo tipo concreto cuyo nombre termine en "Handler".
    /// Una HU nueva no necesita tocar Program.cs: basta crear su Handler y su Validator.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        var handlers = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Handler", StringComparison.Ordinal));
        foreach (var handler in handlers)
            services.AddScoped(handler);

        return services;
    }
}
