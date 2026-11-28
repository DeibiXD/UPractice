using System.Text.RegularExpressions;
using FluentValidation;

namespace UPractice.Application.Features.Perfil;

public sealed partial class ActualizarPerfilAcademicoValidator : AbstractValidator<ActualizarPerfilAcademicoRequest>
{
    public ActualizarPerfilAcademicoValidator()
    {
        // Se valida sin espacios exteriores, igual que se guarda (y que el esquema Zod con .trim()).
        RuleFor(x => x.NumeroCuenta)
            .Must(n => n is not null && NumeroCuentaValido().IsMatch(n.Trim()))
            .WithMessage(MensajesPerfil.NumeroCuenta);
        RuleFor(x => x.CarreraId).NotEmpty().WithMessage(MensajesPerfil.Carrera);
        RuleFor(x => x.IndiceAcademico)
            .NotNull().WithMessage(MensajesPerfil.Indice)
            .InclusiveBetween(0m, 100m).WithMessage(MensajesPerfil.Indice)
            .PrecisionScale(5, 2, ignoreTrailingZeros: true).WithMessage(MensajesPerfil.Indice);
    }

    [GeneratedRegex(@"^\d{6,20}$")]
    private static partial Regex NumeroCuentaValido();
}

public sealed class ActualizarHabilidadesValidator : AbstractValidator<ActualizarHabilidadesRequest>
{
    public ActualizarHabilidadesValidator()
    {
        RuleFor(x => x.HabilidadesIds)
            .NotNull().WithMessage(MensajesPerfil.HabilidadesInvalidas)
            .Must(ids => ids!.Count <= MensajesPerfil.MaximoHabilidades).WithMessage(MensajesPerfil.HabilidadesInvalidas)
            .Must(ids => ids!.All(id => id != Guid.Empty)).WithMessage(MensajesPerfil.HabilidadesInvalidas)
            .Must(ids => ids!.Distinct().Count() == ids!.Count).WithMessage(MensajesPerfil.HabilidadesDuplicadas);
    }
}
