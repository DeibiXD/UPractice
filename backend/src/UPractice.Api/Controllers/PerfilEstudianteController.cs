using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Features.Perfil;

namespace UPractice.Api.Controllers;

/// <summary>Perfil académico, habilidades y privacidad del estudiante (HU-02). Rutas de S5.5.</summary>
[ApiController]
[Route("api/v1/estudiantes/me")]
[Authorize(Roles = "Estudiante")]
[Produces("application/json")]
public sealed class PerfilEstudianteController : ControllerBase
{
    /// <summary>Perfil propio con su estado de verificación. Si aún no lo completa, los datos académicos vienen en null.</summary>
    [HttpGet("perfil")]
    [ProducesResponseType<PerfilEstudianteDto>(StatusCodes.Status200OK)]
    public Task<PerfilEstudianteDto> Obtener([FromServices] ObtenerMiPerfilHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(ct);

    /// <summary>Registra o corrige número de cuenta, carrera e índice. El perfil queda Pendiente de verificación.</summary>
    [HttpPut("perfil/academico")]
    [ProducesResponseType<PerfilEstudianteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<PerfilEstudianteDto> ActualizarAcademico(
        ActualizarPerfilAcademicoRequest request, [FromServices] ActualizarPerfilAcademicoHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);

    /// <summary>Reemplaza la lista de habilidades. Se permite aunque el perfil esté verificado.</summary>
    [HttpPut("habilidades")]
    [ProducesResponseType<PerfilEstudianteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PerfilEstudianteDto> ActualizarHabilidades(
        ActualizarHabilidadesRequest request, [FromServices] ActualizarHabilidadesHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);

    /// <summary>Decide si las empresas a las que se postula ven su índice académico.</summary>
    [HttpPut("privacidad")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActualizarPrivacidad(
        PrivacidadDto request, [FromServices] ActualizarPrivacidadHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(request, ct);
        return NoContent();
    }
}
