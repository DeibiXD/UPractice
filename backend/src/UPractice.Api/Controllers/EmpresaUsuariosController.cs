using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Empresa.Usuarios;

namespace UPractice.Api.Controllers;

[ApiController]
[Route("api/v1/empresa/usuarios")]
[Authorize(Roles = "AdminEmpresa")]
public sealed class EmpresaUsuariosController : ControllerBase
{
    /// <summary>Lista los administradores y reclutadores activos e inactivos de la empresa autenticada.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioEmpresaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<UsuarioEmpresaDto>>> Listar(
        [FromServices] ListarUsuariosEmpresaHandler handler, CancellationToken ct) =>
        Ok(await handler.EjecutarAsync(ct));

    /// <summary>Crea una cuenta y entrega su contraseña temporal una sola vez (D-17).</summary>
    [HttpPost]
    [ProducesResponseType<CuentaCreadaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CuentaCreadaDto>> Crear(
        CrearUsuarioEmpresaRequest request, [FromServices] CrearUsuarioEmpresaHandler handler, CancellationToken ct)
    {
        var cuenta = await handler.EjecutarAsync(request, ct);
        return Created("/api/v1/empresa/usuarios", cuenta);
    }

    /// <summary>Activa o desactiva una cuenta propia de la empresa; impide la autodesactivación.</summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> CambiarEstado(Guid id, CambiarEstadoUsuarioRequest request,
        [FromServices] CambiarEstadoUsuarioEmpresaHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(id, request, ct);
        return NoContent();
    }
}
