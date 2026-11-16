using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Features.Postulaciones;

namespace UPractice.Api.Controllers;

/// <summary>Postulantes de la empresa: lista (HU-E05), detalle (HU-E06) y cambio de estado (HU-E07).</summary>
[ApiController]
[Route("api/v1/empresa")]
[Authorize(Roles = "AdminEmpresa,Reclutador")]
[Produces("application/json")]
public sealed class EmpresaPostulacionesController : ControllerBase
{
    /// <summary>Postulantes de una vacante de la empresa, la más reciente primero. Una vacante ajena responde 404.</summary>
    [HttpGet("vacantes/{vacanteId:guid}/postulantes")]
    [ProducesResponseType<List<PostulanteResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<List<PostulanteResumenDto>> Listar(Guid vacanteId, [FromServices] ListarPostulantesHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(vacanteId, ct);

    /// <summary>Detalle del postulante: perfil verificado, CV adjunto e historial. Una postulación ajena responde 404.</summary>
    [HttpGet("postulaciones/{id:guid}")]
    [ProducesResponseType<PostulanteDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<PostulanteDetalleDto> Detalle(Guid id, [FromServices] ObtenerPostulanteHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, ct);

    /// <summary>Cambia el estado a EnRevision, Entrevista, Oferta o Rechazada, con un comentario opcional.</summary>
    [HttpPut("postulaciones/{id:guid}/estado")]
    [ProducesResponseType<PostulanteDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<PostulanteDetalleDto> CambiarEstado(
        Guid id, CambiarEstadoPostulacionRequest request, [FromServices] CambiarEstadoPostulacionHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, request, ct);
}
