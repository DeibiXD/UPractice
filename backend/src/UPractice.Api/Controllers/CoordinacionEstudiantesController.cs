using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Coordinacion.Verificacion;

namespace UPractice.Api.Controllers;

/// <summary>Verificación de perfiles académicos por la coordinación (HU-C02). Solo estudiantes de su universidad.</summary>
[ApiController]
[Route("api/v1/coordinacion/estudiantes")]
[Authorize(Roles = "Coordinador")]
[Produces("application/json")]
public sealed class CoordinacionEstudiantesController : ControllerBase
{
    /// <summary>Perfiles de los estudiantes de su universidad, filtrables por estado (ej. Pendiente), paginados.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaResultado<EstudianteVerificacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PaginaResultado<EstudianteVerificacionDto>> Listar(
        [FromQuery] ListarEstudiantesVerificacionRequest request,
        [FromServices] ListarEstudiantesVerificacionHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);

    /// <summary>Verifica o rechaza (con observación obligatoria) un perfil Pendiente.</summary>
    [HttpPatch("{usuarioId:guid}/verificacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Resolver(
        Guid usuarioId, ResolverVerificacionRequest request,
        [FromServices] ResolverVerificacionHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(usuarioId, request, ct);
        return NoContent();
    }
}
