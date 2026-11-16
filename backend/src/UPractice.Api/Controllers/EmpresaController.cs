using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Features.Empresa.Habilitaciones;

namespace UPractice.Api.Controllers;

/// <summary>Solicitudes de habilitación de la empresa autenticada.</summary>
[ApiController]
[Route("api/v1/empresa/habilitaciones")]
[Authorize(Roles = "AdminEmpresa")]
[Produces("application/json")]
public sealed class EmpresaController : ControllerBase
{
    /// <summary>Lista las habilitaciones de la empresa del claim JWT.</summary>
    [HttpGet]
    [EndpointSummary("Lista las habilitaciones de la empresa autenticada")]
    [ProducesResponseType<IReadOnlyList<HabilitacionEmpresaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public Task<IReadOnlyList<HabilitacionEmpresaDto>> Habilitaciones(
        [FromServices] ListarHabilitacionesEmpresaHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(ct);

    /// <summary>Solicita una habilitación nueva o reintenta una solicitud rechazada.</summary>
    [HttpPost]
    [EndpointSummary("Solicita una habilitación universitaria")]
    [ProducesResponseType<HabilitacionEmpresaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HabilitacionEmpresaDto>> SolicitarHabilitacion(
        SolicitarHabilitacionRequest request,
        [FromServices] SolicitarHabilitacionEmpresaHandler handler,
        CancellationToken ct)
    {
        var respuesta = await handler.EjecutarAsync(request, ct);
        return Created("/api/v1/empresa/habilitaciones", respuesta);
    }
}
