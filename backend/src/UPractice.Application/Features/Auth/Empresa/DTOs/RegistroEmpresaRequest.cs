namespace UPractice.Application.Features.Auth.Empresa;

/// <summary>Datos para registrar una empresa, su administrador y habilitaciones iniciales.</summary>
public sealed record RegistroEmpresaRequest(
    string NombreComercial,
    string RazonSocial,
    string Rtn,
    string Sector,
    string? SitioWeb,
    IReadOnlyList<Guid> UniversidadesIds,
    AdminEmpresaRegistroRequest Admin);

/// <summary>Datos de la cuenta administradora creada junto con la empresa.</summary>
public sealed record AdminEmpresaRegistroRequest(
    string Nombre1,
    string? Nombre2,
    string Apellido1,
    string? Apellido2,
    string Correo,
    string Password,
    string ConfirmarPassword);
