using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Features.Postulaciones;
using UPractice.Domain.Enums;

namespace UPractice.Api.Controllers;

/// <summary>Postulaciones del estudiante: postularse (HU-05) y su panel (HU-06). Rutas de S5.5.</summary>
[ApiController]
[Route("api/v1/estudiantes/me/postulaciones")]
[Authorize(Roles = "Estudiante")]
[Produces("application/json")]
public sealed class PostulacionesController : ControllerBase
{
    /// <summary>Se postula a una vacante con uno de sus CV. Exige perfil verificado y carrera elegible.</summary>
    [HttpPost("~/api/v1/vacantes/{vacanteId:guid}/postulaciones")]
    [ProducesResponseType<MiPostulacionDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MiPostulacionDetalleDto>> Postularse(
        Guid vacanteId, CrearPostulacionRequest request, [FromServices] PostularseHandler handler, CancellationToken ct)
    {
        var postulacion = await handler.EjecutarAsync(vacanteId, request, ct);
        return CreatedAtAction(nameof(Detalle), new { id = postulacion.Id }, postulacion);
    }

    /// <summary>Panel del estudiante: sus postulaciones, la más reciente primero, con las acciones que puede hacer en cada una.</summary>
    /// <param name="estado">Opcional: solo las postulaciones en ese estado.</param>
    [HttpGet]
    [ProducesResponseType<List<MiPostulacionDto>>(StatusCodes.Status200OK)]
    public Task<List<MiPostulacionDto>> Listar(
        [FromQuery] EstadoPostulacion? estado, [FromServices] ListarMisPostulacionesHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(estado, ct);

    /// <summary>Detalle de una postulación propia, con su historial.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MiPostulacionDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<MiPostulacionDetalleDto> Detalle(Guid id, [FromServices] ObtenerMiPostulacionHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, ct);

    /// <summary>Retira la postulación (solo antes de recibir una oferta).</summary>
    [HttpPost("{id:guid}/retirar")]
    [ProducesResponseType<MiPostulacionDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<MiPostulacionDetalleDto> Retirar(Guid id, [FromServices] RetirarPostulacionHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, ct);

    /// <summary>Reactiva una postulación retirada, con el mismo CV u otro.</summary>
    [HttpPost("{id:guid}/reactivar")]
    [ProducesResponseType<MiPostulacionDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<MiPostulacionDetalleDto> Reactivar(
        Guid id, ReactivarPostulacionRequest request, [FromServices] ReactivarPostulacionHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, request, ct);

    /// <summary>Acepta o declina una oferta. Al aceptar, sus demás postulaciones activas se retiran solas.</summary>
    [HttpPost("{id:guid}/respuesta-oferta")]
    [ProducesResponseType<MiPostulacionDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<MiPostulacionDetalleDto> ResponderOferta(
        Guid id, ResponderOfertaRequest request, [FromServices] ResponderOfertaHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, request, ct);
}
