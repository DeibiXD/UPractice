using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Auth;

// DTO de S5.6 (Auth). En el JSON salen en camelCase y los enums como texto.

public sealed record Referencia(Guid Id, string Nombre);

public sealed record UsuarioActualDto(
    Guid Id,
    string NombreCompleto,
    string Correo,
    Rol Rol,
    Referencia? Empresa,
    Referencia? Universidad,
    bool DebeCambiarPassword)
{
    /// <summary>Requiere Empresa y Universidad cargadas (Include) si el usuario las tiene.</summary>
    public static UsuarioActualDto Desde(Usuario u) => new(
        u.Id, u.NombreCompleto, u.Correo, u.Rol,
        u.Empresa is null ? null : new Referencia(u.Empresa.Id, u.Empresa.NombreComercial),
        u.Universidad is null ? null : new Referencia(u.Universidad.Id, u.Universidad.Siglas),
        u.DebeCambiarPassword);
}

public sealed record LoginRequest(string Correo, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiraEn, UsuarioActualDto Usuario);

public sealed record CambiarPasswordRequest(string PasswordActual, string PasswordNuevo, string ConfirmarPassword);
