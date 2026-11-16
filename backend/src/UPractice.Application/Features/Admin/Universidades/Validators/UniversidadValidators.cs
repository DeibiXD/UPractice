using FluentValidation;
using UPractice.Application.Features.Admin.Universidades.DTOs;

namespace UPractice.Application.Features.Admin.Universidades.Validators;

internal static class ReglasUniversidad
{
    private const string PatronSiglas = @"^[A-Za-z0-9]{2,15}$";
    private const string PatronDominio =
        @"^(?=.{1,100}$)(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}$";

    internal static bool SiglasValidas(string? valor) =>
        valor is not null && System.Text.RegularExpressions.Regex.IsMatch(valor.Trim(), PatronSiglas);

    internal static bool DominioValido(string? valor) =>
        valor is not null && System.Text.RegularExpressions.Regex.IsMatch(valor.Trim(), PatronDominio);
}

public sealed class CrearUniversidadValidator : AbstractValidator<CrearUniversidadRequestDto>
{
    public CrearUniversidadValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Siglas)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(ReglasUniversidad.SiglasValidas)
            .WithMessage("Las siglas deben tener entre 2 y 15 letras o números.");
        RuleFor(x => x.DominioCorreo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Trim().Length <= 100 && ReglasUniversidad.DominioValido(value))
            .WithMessage("Ingrese un dominio de correo válido, sin @.");
    }
}

public sealed class ActualizarUniversidadValidator : AbstractValidator<ActualizarUniversidadRequestDto>
{
    public ActualizarUniversidadValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Siglas)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(ReglasUniversidad.SiglasValidas)
            .WithMessage("Las siglas deben tener entre 2 y 15 letras o números.");
        RuleFor(x => x.DominioCorreo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value.Trim().Length <= 100 && ReglasUniversidad.DominioValido(value))
            .WithMessage("Ingrese un dominio de correo válido, sin @.");
    }
}

public sealed class CambiarEstadoUniversidadValidator : AbstractValidator<CambiarEstadoUniversidadRequestDto>
{
    public CambiarEstadoUniversidadValidator() =>
        RuleFor(x => x.Activa)
            .NotNull()
            .WithMessage("Indique si la universidad estará activa.");
}
