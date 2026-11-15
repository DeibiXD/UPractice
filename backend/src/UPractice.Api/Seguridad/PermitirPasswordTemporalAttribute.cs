namespace UPractice.Api.Seguridad;

/// <summary>
/// Marca las acciones que una cuenta con contraseña temporal sí puede usar (D-17):
/// solo GET /auth/me y PUT /auth/password.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class PermitirPasswordTemporalAttribute : Attribute;
