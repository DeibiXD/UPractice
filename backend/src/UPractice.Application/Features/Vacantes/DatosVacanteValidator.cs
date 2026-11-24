using FluentValidation;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Vacantes;

/// <summary>Campos compartidos de creación y edición, sin opciones de publicación.</summary>
public interface IDatosVacanteRequest
{
    string Titulo { get; }
    string Descripcion { get; }
    Modalidad Modalidad { get; }
    string Ubicacion { get; }
    int Cupos { get; }
    bool Remunerada { get; }
    decimal? IndiceMinimo { get; }
    DateOnly FechaCierre { get; }
    Guid[] CarrerasIds { get; }
    HabilidadVacanteRequest[] Habilidades { get; }
}

public abstract class DatosVacanteValidator<T> : AbstractValidator<T> where T : IDatosVacanteRequest
{
    protected DatosVacanteValidator(IFechaHonduras fecha)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Titulo).NotEmpty().WithMessage(MensajesVacantes.Titulo)
            .Must(x => x.Trim().Length is >= 5 and <= 120).WithMessage(MensajesVacantes.Titulo);
        RuleFor(x => x.Descripcion).NotEmpty().WithMessage(MensajesVacantes.Descripcion)
            .Must(x => x.Trim().Length is >= 30 and <= 4000).WithMessage(MensajesVacantes.Descripcion);
        RuleFor(x => x.Ubicacion).NotEmpty().WithMessage(MensajesVacantes.Ubicacion)
            .Must(x => x.Trim().Length is >= 2 and <= 120).WithMessage(MensajesVacantes.Ubicacion);
        RuleFor(x => x.Modalidad).IsInEnum().WithMessage(MensajesVacantes.Modalidad);
        RuleFor(x => x.Cupos).InclusiveBetween(1, 50).WithMessage(MensajesVacantes.Cupos);
        RuleFor(x => x.IndiceMinimo).InclusiveBetween(0, 100)
            .When(x => x.IndiceMinimo is not null).WithMessage(MensajesVacantes.IndiceMinimo);
        RuleFor(x => x.FechaCierre).GreaterThan(_ => fecha.Hoy()).WithMessage(MensajesVacantes.FechaCierre);
        RuleFor(x => x.CarrerasIds).NotEmpty().WithMessage(MensajesVacantes.CarrerasRequeridas);
        RuleFor(x => x.CarrerasIds)
            .Must(ids => ids.All(id => id != Guid.Empty)).WithMessage(MensajesVacantes.CarrerasInvalidas)
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage(MensajesVacantes.CarrerasDuplicadas)
            .When(x => x.CarrerasIds is not null);
        RuleFor(x => x.Habilidades).NotNull().WithMessage(MensajesVacantes.HabilidadesInvalidas);
        RuleFor(x => x.Habilidades)
            .Must(items => items.All(x => x is not null && x.HabilidadId != Guid.Empty))
            .WithMessage(MensajesVacantes.HabilidadesInvalidas)
            .Must(items => items.Select(x => x.HabilidadId).Distinct().Count() == items.Length)
            .WithMessage(MensajesVacantes.HabilidadesDuplicadas)
            .When(x => x.Habilidades is not null);
    }
}
