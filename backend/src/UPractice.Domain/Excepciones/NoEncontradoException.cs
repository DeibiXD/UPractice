namespace UPractice.Domain.Excepciones;

/// <summary>Recurso inexistente o fuera del alcance del usuario (aislamiento por tenant). Se convierte en 404.</summary>
public sealed class NoEncontradoException(string message) : Exception(message);
