using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Empresa.Habilitaciones;

/// <summary>Universidad asociada a una habilitación.</summary>
public sealed record UniversidadHabilitacionDto(Guid Id, string Nombre);

/// <summary>Estado de una solicitud de habilitación de empresa.</summary>
public sealed record HabilitacionEmpresaDto(
    Guid Id,
    UniversidadHabilitacionDto Universidad,
    EstadoHabilitacion Estado,
    string? MotivoRechazo,
    DateTime FechaSolicitud,
    DateTime? FechaResolucion);

/// <summary>Solicitud de habilitación para una universidad.</summary>
public sealed record SolicitarHabilitacionRequest(Guid UniversidadId);
