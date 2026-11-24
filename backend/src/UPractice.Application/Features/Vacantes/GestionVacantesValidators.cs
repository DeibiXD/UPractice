using FluentValidation;
using UPractice.Application.Abstracciones;

namespace UPractice.Application.Features.Vacantes;

public sealed class ActualizarVacanteValidator(IFechaHonduras fecha) : DatosVacanteValidator<ActualizarVacanteRequest>(fecha)
{
}

public sealed class ListarVacantesValidator : AbstractValidator<ListarVacantesRequest>
{
    public ListarVacantesValidator()
    {
        RuleFor(x => x.Estado).IsInEnum().WithMessage(MensajesVacantes.Estado);
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1).WithMessage(MensajesVacantes.Pagina);
        RuleFor(x => x.TamanoPagina).InclusiveBetween(1, 50).WithMessage(MensajesVacantes.TamanoPagina);
    }
}

public sealed class CambiarEstadoVacanteValidator : AbstractValidator<CambiarEstadoVacanteRequest>
{
    public CambiarEstadoVacanteValidator() =>
        RuleFor(x => x.NuevoEstado).IsInEnum().WithMessage(MensajesVacantes.Estado);
}
