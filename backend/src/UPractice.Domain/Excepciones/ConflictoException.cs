namespace UPractice.Domain.Excepciones;

/// <summary>Conflicto con el estado actual (duplicados, transición inválida). Se convierte en 409 con <see cref="Code"/>.</summary>
public sealed class ConflictoException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
