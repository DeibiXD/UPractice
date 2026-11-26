using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Catalogos;

namespace UPractice.Api.Controllers;

/// <summary>Catálogos públicos usados por los formularios de registro.</summary>
[ApiController]
[Route("api/v1/catalogos")]
[Produces("application/json")]
public sealed class CatalogosController : ControllerBase
{
    /// <summary>Devuelve únicamente las universidades activas.</summary>
    [HttpGet("universidades")]
    [EndpointSummary("Lista las universidades activas disponibles")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<UniversidadDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UniversidadDto>> Universidades(
        [FromServices] ListarUniversidadesHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(ct);

    /// <summary>Carreras activas de universidades activas; con universidadId, solo las de esa universidad.</summary>
    [HttpGet("carreras")]
    [EndpointSummary("Lista las carreras activas (opcionalmente de una universidad)")]
    [ProducesResponseType<IReadOnlyList<CarreraDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CarreraDto>> Carreras(
        [FromQuery] Guid? universidadId,
        [FromServices] ListarCarrerasHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(universidadId, ct);

    /// <summary>Catálogo de habilidades, ordenado por nombre.</summary>
    [HttpGet("habilidades")]
    [EndpointSummary("Lista las habilidades del catálogo")]
    [ProducesResponseType<IReadOnlyList<HabilidadDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HabilidadDto>> Habilidades(
        [FromServices] ListarHabilidadesHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(ct);
}
