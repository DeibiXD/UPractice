using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Curriculums;
using UPractice.Application.Features.Postulaciones;
using UPractice.Domain.Enums;

namespace UPractice.Api.Controllers;

/// <summary>Postulantes de la empresa: lista (HU-E05), detalle (HU-E06) y cambio de estado (HU-E07). Rutas de S5.5.</summary>
[ApiController]
[Route("api/v1/empresa")]
[Authorize(Roles = "AdminEmpresa,Reclutador")]
[Produces("application/json")]
public sealed class EmpresaPostulacionesController : ControllerBase
{
    /// <summary>Postulaciones a una vacante de la empresa, la más reciente primero. Una vacante ajena responde 404.</summary>
    /// <param name="estado">Opcional: solo las que están en ese estado.</param>
    /// <param name="cumpleRequisitos">Opcional: solo las que cumplen (o no) el índice mínimo.</param>
    [HttpGet("vacantes/{vacanteId:guid}/postulaciones")]
    [ProducesResponseType<List<PostulanteResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<List<PostulanteResumenDto>> Listar(
        Guid vacanteId, [FromQuery] EstadoPostulacion? estado, [FromQuery] bool? cumpleRequisitos,
        [FromServices] ListarPostulantesHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(vacanteId, estado, cumpleRequisitos, ct);

    /// <summary>Detalle del postulante: perfil verificado, CV adjunto e historial. Una postulación ajena responde 404.</summary>
    [HttpGet("postulaciones/{id:guid}")]
    [ProducesResponseType<PostulanteDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<PostulanteDetalleDto> Detalle(Guid id, [FromServices] ObtenerPostulanteHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, ct);

    /// <summary>URL firmada (5 minutos) del CV adjunto a la postulación, aunque el estudiante lo haya eliminado después.</summary>
    [HttpGet("postulaciones/{id:guid}/curriculum/descarga")]
    [ProducesResponseType<UrlDescargaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UrlDescargaDto> DescargarCurriculum(
        Guid id, [FromServices] DescargarCurriculumPostulacionHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, ct);

    /// <summary>Cambia el estado a EnRevision, Entrevista, Oferta o Rechazada, con un comentario opcional.</summary>
    [HttpPatch("postulaciones/{id:guid}/estado")]
    [ProducesResponseType<PostulanteDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<PostulanteDetalleDto> CambiarEstado(
        Guid id, CambiarEstadoPostulacionRequest request, [FromServices] CambiarEstadoPostulacionHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, request, ct);
}
