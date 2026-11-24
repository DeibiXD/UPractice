using FluentValidation;
using UPractice.Application.Comun;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Empresa.Usuarios;

public sealed class CrearUsuarioEmpresaValidator : AbstractValidator<CrearUsuarioEmpresaRequest>
{
    public CrearUsuarioEmpresaValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        // Validar los valores que se guardarán, sin los espacios exteriores del formulario.
        RuleFor(x => Recortar(x.Nombre1)!).NombreObligatorio().OverridePropertyName("nombre1");
        RuleFor(x => Recortar(x.Nombre2)).NombreOpcional().OverridePropertyName("nombre2");
        RuleFor(x => Recortar(x.Apellido1)!).NombreObligatorio().OverridePropertyName("apellido1");
        RuleFor(x => Recortar(x.Apellido2)).NombreOpcional().OverridePropertyName("apellido2");
        RuleFor(x => Recortar(x.Correo)!).CorreoValido().OverridePropertyName("correo");
        RuleFor(x => x.Rol).Must(rol => rol is Rol.AdminEmpresa or Rol.Reclutador)
            .WithMessage("Seleccione un rol permitido.");
    }

    private static string? Recortar(string? valor) => valor?.Trim();
}
