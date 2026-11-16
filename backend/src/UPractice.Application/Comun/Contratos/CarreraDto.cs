namespace UPractice.Application.Comun.Contratos;

/// <summary>Referencia académica compartida por vacantes y catálogos.</summary>
public sealed record CarreraDto(Guid Id, string Nombre, string Facultad, Guid UniversidadId);
