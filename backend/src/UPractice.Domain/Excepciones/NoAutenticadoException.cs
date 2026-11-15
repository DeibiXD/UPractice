namespace UPractice.Domain.Excepciones;

/// <summary>
/// Credenciales incorrectas, cuenta inactiva o empresa suspendida. Se convierte en 401 con un mensaje
/// genérico: nunca revela si el correo existe (HU-01).
/// </summary>
public sealed class NoAutenticadoException(string message = "Correo o contraseña incorrectos.") : Exception(message);
