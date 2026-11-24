namespace UPractice.Application.Comun.Contratos;

/// <summary>La contraseña temporal solo se entrega en la respuesta de creación.</summary>
public sealed record CuentaCreadaDto(Guid UsuarioId, string Correo, string PasswordTemporal);
