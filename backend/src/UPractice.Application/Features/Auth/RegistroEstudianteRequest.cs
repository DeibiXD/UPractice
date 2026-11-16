using FluentValidation;
using UPractice.Application.Comun;

namespace UPractice.Application.Features.Auth;

/// <summary>Datos requeridos para crear una cuenta de estudiante.</summary>
public sealed record RegistroEstudianteRequest(
    string Nombre1,
    string? Nombre2,
    string Apellido1,
    string? Apellido2,
    string Correo,
    string Password,
    string ConfirmarPassword);

public sealed class RegistroEstudianteValidator : AbstractValidator<RegistroEstudianteRequest>
{
    public RegistroEstudianteValidator()
    {
        RuleFor(x => x.Nombre1).NombreObligatorio();
        RuleFor(x => x.Nombre2).NombreOpcional();
        RuleFor(x => x.Apellido1).NombreObligatorio();
        RuleFor(x => x.Apellido2).NombreOpcional();
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.Password).PasswordSegura();
        RuleFor(x => x.ConfirmarPassword)
            .Equal(x => x.Password)
            .WithMessage(ReglasValidacion.MensajeConfirmar);
    }
}
