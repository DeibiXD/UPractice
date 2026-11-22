using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Auth;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Vacantes;

public sealed record FiltroVacantesQuery(
    string? Busqueda = null,
    Guid? CarreraId = null,
    Modalidad? Modalidad = null,
    bool? Remunerada = null,
    int Pagina = 1,
    int TamanoPagina = 10);

public record VacanteResumenDto(
    Guid Id,
    string Titulo,
    Referencia Empresa,
    Modalidad Modalidad,
    string Ubicacion,
    bool Remunerada,
    IReadOnlyList<string> Carreras,
    DateTime FechaPublicacion,
    DateOnly FechaCierre,
    bool CumploRequisitos,
    bool YaPostulado);

public sealed record VacanteDetalleDto(
    Guid Id,
    string Titulo,
    Referencia Empresa,
    Modalidad Modalidad,
    string Ubicacion,
    bool Remunerada,
    IReadOnlyList<string> Carreras,
    DateTime FechaPublicacion,
    DateOnly FechaCierre,
    bool CumploRequisitos,
    bool YaPostulado,
    string Descripcion,
    int Cupos,
    IReadOnlyList<CarreraDto> CarrerasDetalle,
    decimal? IndiceMinimo,
    IReadOnlyList<HabilidadVacanteDto> Habilidades)
    : VacanteResumenDto(Id, Titulo, Empresa, Modalidad, Ubicacion, Remunerada, Carreras,
        FechaPublicacion, FechaCierre, CumploRequisitos, YaPostulado);
