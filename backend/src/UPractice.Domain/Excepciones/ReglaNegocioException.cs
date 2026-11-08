namespace UPractice.Domain.Excepciones;

/// <summary>Regla de negocio incumplida. El middleware la convierte en 422 con <see cref="Code"/>.</summary>
public sealed class ReglaNegocioException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
