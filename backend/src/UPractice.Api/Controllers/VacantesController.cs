using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Vacantes;

namespace UPractice.Api.Controllers;

/// <summary>Exploración y detalle de vacantes disponibles para la universidad del estudiante (HU-04).</summary>
[ApiController]
[Route("api/v1/vacantes")]
[Authorize(Roles = "Estudiante")]
public sealed class VacantesController : ControllerBase
{
    /// <summary>Busca vacantes vigentes; filtros combinables, página 1, tamaño 10 y máximo 50.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaResultado<VacanteResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public Task<PaginaResultado<VacanteResumenDto>> Listar([FromQuery] FiltroVacantesQuery filtro,
        [FromServices] ExplorarVacantesHandler handler, CancellationToken ct) => handler.EjecutarAsync(filtro, ct);

    /// <summary>Consulta el detalle de una vacante disponible; una vacante ajena al catálogo devuelve 404.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<VacanteDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<VacanteDetalleDto> Obtener(Guid id, [FromServices] ObtenerVacanteEstudianteHandler handler, CancellationToken ct) =>
        handler.EjecutarAsync(id, ct);
}
