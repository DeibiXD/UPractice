using System.Text.Json.Serialization;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Vacantes;

public sealed record HabilidadVacanteRequest(Guid HabilidadId, [property: JsonRequired] bool EsRequerida);

public sealed record CrearVacanteRequest(
    string Titulo,
    string Descripcion,
    [property: JsonRequired] Modalidad Modalidad,
    string Ubicacion,
    int Cupos,
    [property: JsonRequired] bool Remunerada,
    decimal? IndiceMinimo,
    DateOnly FechaCierre,
    Guid[] CarrerasIds,
    HabilidadVacanteRequest[] Habilidades,
    [property: JsonRequired] bool Publicar) : IDatosVacanteRequest;

public sealed record ActualizarVacanteRequest(
    string Titulo,
    string Descripcion,
    [property: JsonRequired] Modalidad Modalidad,
    string Ubicacion,
    int Cupos,
    [property: JsonRequired] bool Remunerada,
    decimal? IndiceMinimo,
    DateOnly FechaCierre,
    Guid[] CarrerasIds,
    HabilidadVacanteRequest[] Habilidades) : IDatosVacanteRequest;

public sealed record ListarVacantesRequest(EstadoVacante? Estado = null, int Pagina = 1, int TamanoPagina = 10);

public sealed record CambiarEstadoVacanteRequest([property: JsonRequired] EstadoVacante NuevoEstado);

public sealed record HabilidadVacanteDto(Guid Id, string Nombre, bool EsRequerida);

public sealed record VacanteEmpresaDto(
    Guid Id,
    string Titulo,
    string Descripcion,
    Modalidad Modalidad,
    string Ubicacion,
    int Cupos,
    bool Remunerada,
    decimal? IndiceMinimo,
    DateOnly FechaCierre,
    DateTime? FechaPublicacion,
    EstadoVacante Estado,
    IReadOnlyList<CarreraDto> Carreras,
    IReadOnlyList<HabilidadVacanteDto> Habilidades,
    string CreadaPor,
    int TotalPostulaciones,
    IReadOnlyDictionary<EstadoPostulacion, int> PostulacionesPorEstado);
