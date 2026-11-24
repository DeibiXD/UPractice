using UPractice.Application.Abstracciones;

namespace UPractice.Application.Features.Vacantes;

public sealed class CrearVacanteValidator(IFechaHonduras fecha) : DatosVacanteValidator<CrearVacanteRequest>(fecha)
{
}
