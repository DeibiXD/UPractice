namespace UPractice.Application.Features.Admin.Universidades.DTOs;

/// <summary>Datos para registrar una universidad.</summary>
public sealed record CrearUniversidadRequestDto(string Nombre, string Siglas, string DominioCorreo);

/// <summary>Datos editables de una universidad existente.</summary>
public sealed record ActualizarUniversidadRequestDto(string Nombre, string Siglas, string DominioCorreo);

/// <summary>Estado activo solicitado para una universidad.</summary>
public sealed record CambiarEstadoUniversidadRequestDto(bool? Activa);

/// <summary>Universidad y conteos de usuarios visibles para SuperAdmin.</summary>
public sealed record UniversidadAdminResponseDto(
    Guid Id,
    string Nombre,
    string Siglas,
    string DominioCorreo,
    bool Activa,
    int TotalEstudiantes,
    int TotalCoordinadores);
