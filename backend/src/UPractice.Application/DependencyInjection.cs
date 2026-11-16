using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Empresa.Habilitaciones;

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

        // Llaves de error en camelCase y con el mismo nombre del campo del formulario (S5.3):
        // "titulo", "admin.correo", "habilidades[0].habilidadId".
        ValidatorOptions.Global.PropertyNameResolver = (_, miembro, _) => miembro is null ? null : CamelCase(miembro.Name);
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<IEmpresaPublicacionGuard, EmpresaPublicacionGuard>();

        var handlers = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Handler", StringComparison.Ordinal));
        foreach (var handler in handlers)
            services.AddScoped(handler);

        return services;
    }

    public static string CamelCase(string nombre) =>
        string.IsNullOrEmpty(nombre) || char.IsLower(nombre[0]) ? nombre : char.ToLowerInvariant(nombre[0]) + nombre[1..];
}
