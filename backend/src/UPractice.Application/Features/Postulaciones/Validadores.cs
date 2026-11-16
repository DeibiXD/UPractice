using FluentValidation;

namespace UPractice.Application.Features.Postulaciones;

// Mensajes de docs/mensajes-validacion.md (idénticos a los del esquema Zod del frontend).

public sealed class CrearPostulacionValidator : AbstractValidator<CrearPostulacionRequest>
{
    public CrearPostulacionValidator()
    {
        RuleFor(x => x.CurriculumId).NotEmpty().WithMessage(MensajesPostulacion.CurriculumObligatorio);
    }
}

public sealed class ReactivarPostulacionValidator : AbstractValidator<ReactivarPostulacionRequest>
{
    public ReactivarPostulacionValidator()
    {
        RuleFor(x => x.CurriculumId).NotEmpty().WithMessage(MensajesPostulacion.CurriculumObligatorio);
    }
}

public sealed class ResponderOfertaValidator : AbstractValidator<ResponderOfertaRequest>
{
    public ResponderOfertaValidator()
    {
        RuleFor(x => x.Aceptar).NotNull().WithMessage(MensajesPostulacion.RespuestaObligatoria);
    }
}

public sealed class CambiarEstadoPostulacionValidator : AbstractValidator<CambiarEstadoPostulacionRequest>
{
    public CambiarEstadoPostulacionValidator()
    {
        RuleFor(x => x.Comentario).MaximumLength(500).WithMessage(MensajesPostulacion.ComentarioMaximo);
    }
}

internal static class MensajesPostulacion
{
    public const string CurriculumObligatorio = "Seleccione un currículum.";
    public const string RespuestaObligatoria = "Indique si acepta o declina la oferta.";
    public const string ComentarioMaximo = "Máximo 500 caracteres.";
}
