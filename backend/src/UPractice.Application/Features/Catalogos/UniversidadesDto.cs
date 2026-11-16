namespace UPractice.Application.Features.Catalogos;

/// <summary>Universidad disponible para registro de estudiantes.</summary>
public sealed record UniversidadDto(Guid Id, string Nombre, string Siglas, string DominioCorreo);
