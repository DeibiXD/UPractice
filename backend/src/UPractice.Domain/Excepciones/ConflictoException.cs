namespace UPractice.Domain.Excepciones;

/// <summary>
/// Conflicto de unicidad (correo, RTN, postulación duplicada, siglas, dominio). Se convierte en 409 con <see cref="Code"/>.
/// Una transición de estado no permitida NO es un conflicto: es 422 TRANSICION_INVALIDA (<see cref="ReglaNegocioException"/>).
/// </summary>
public sealed class ConflictoException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
