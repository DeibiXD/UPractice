using FluentValidation;

namespace UPractice.Application.Features.Vacantes;

public sealed class FiltroVacantesValidator : AbstractValidator<FiltroVacantesQuery>
{
    public FiltroVacantesValidator()
    {
        RuleFor(x => x.CarreraId).NotEqual(Guid.Empty).WithMessage("Seleccione una carrera válida.");
        RuleFor(x => x.Modalidad).IsInEnum().WithMessage(MensajesVacantes.Modalidad);
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");
        RuleFor(x => x.TamanoPagina).InclusiveBetween(1, 50).WithMessage("El tamaño de página debe estar entre 1 y 50.");
    }
}
