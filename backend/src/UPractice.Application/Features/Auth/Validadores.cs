using FluentValidation;
using UPractice.Application.Comun;

namespace UPractice.Application.Features.Auth;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingrese su contraseña.");
    }
}

public sealed class CambiarPasswordValidator : AbstractValidator<CambiarPasswordRequest>
{
    public CambiarPasswordValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty().WithMessage("Ingrese su contraseña actual.");
        RuleFor(x => x.PasswordNuevo).PasswordSegura();
        RuleFor(x => x.ConfirmarPassword).Equal(x => x.PasswordNuevo).WithMessage(ReglasValidacion.MensajeConfirmar);
    }
}
