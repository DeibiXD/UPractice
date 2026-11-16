using FluentValidation;
using UPractice.Application.Abstracciones;

namespace UPractice.Application.Features.Empresa.Habilitaciones;

public sealed class SolicitarHabilitacionValidator : AbstractValidator<SolicitarHabilitacionRequest>
{
    public SolicitarHabilitacionValidator(IEmpresaHabilitacionRepository repositorio)
    {
        RuleFor(x => x.UniversidadId)
            .Cascade(CascadeMode.Stop)
            .NotEqual(Guid.Empty)
            .WithMessage("Seleccione una universidad válida.")
            .MustAsync(async (id, ct) => await repositorio.ObtenerUniversidadActivaAsync(id, ct) is not null)
            .WithMessage("Seleccione una universidad válida.");
    }
}
