using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Vacantes;

namespace UPractice.Api.Controllers;

[ApiController]
[Route("api/v1/empresa/vacantes")]
[Authorize(Roles = "AdminEmpresa,Reclutador")]
public sealed class EmpresaVacantesController : ControllerBase
{
    /// <summary>Lista vacantes propias con estado efectivo y conteos; página 1, tamaño 10, máximo 50.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaResultado<VacanteEmpresaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public Task<PaginaResultado<VacanteEmpresaDto>> Listar([FromQuery] ListarVacantesRequest request,
        [FromServices] ListarVacantesHandler handler, CancellationToken ct) => handler.EjecutarAsync(request, ct);

    /// <summary>Consulta una vacante propia con carreras, habilidades y conteos de postulaciones.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<VacanteEmpresaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public Task<VacanteEmpresaDto> Obtener(Guid id, [FromServices] ObtenerVacanteHandler handler, CancellationToken ct) =>
        handler.EjecutarAsync(id, ct);

    /// <summary>Crea un borrador o publica una vacante de la empresa autenticada.</summary>
    [HttpPost]
    [ProducesResponseType<VacanteEmpresaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<VacanteEmpresaDto>> Crear(
        CrearVacanteRequest request, [FromServices] CrearVacanteHandler handler, CancellationToken ct)
    {
        var vacante = await handler.EjecutarAsync(request, ct);
        return Created($"/api/v1/empresa/vacantes/{vacante.Id}", vacante);
    }

    /// <summary>Edita únicamente una vacante propia en Borrador o Pausada; conserva autor y publicación.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<VacanteEmpresaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public Task<VacanteEmpresaDto> Actualizar(Guid id, ActualizarVacanteRequest request,
        [FromServices] ActualizarVacanteHandler handler, CancellationToken ct) => handler.EjecutarAsync(id, request, ct);

    /// <summary>Publica, pausa, reanuda o cierra según TransicionesVacante; publicar vuelve a comprobar requisitos.</summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoVacanteRequest request,
        [FromServices] CambiarEstadoVacanteHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(id, request, ct);
        return NoContent();
    }
}
