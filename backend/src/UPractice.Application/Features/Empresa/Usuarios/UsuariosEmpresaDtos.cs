using System.Text.Json.Serialization;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Empresa.Usuarios;

public sealed record UsuarioEmpresaDto(
    Guid Id, string NombreCompleto, string Correo, Rol Rol, bool Activo);

public sealed record CrearUsuarioEmpresaRequest(
    string Nombre1,
    string? Nombre2,
    string Apellido1,
    string? Apellido2,
    string Correo,
    [property: JsonRequired] Rol Rol);

public sealed record CambiarEstadoUsuarioRequest([property: JsonRequired] bool Activo);
