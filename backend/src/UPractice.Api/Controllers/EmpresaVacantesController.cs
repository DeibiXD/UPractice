using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Features.Vacantes;

namespace UPractice.Api.Controllers;

[ApiController]
[Route("api/v1/empresa/vacantes")]
[Authorize(Roles = "AdminEmpresa,Reclutador")]
public sealed class EmpresaVacantesController : ControllerBase
{
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
}
