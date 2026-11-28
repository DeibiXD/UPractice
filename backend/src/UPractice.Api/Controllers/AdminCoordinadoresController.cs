using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Admin.Coordinadores;

namespace UPractice.Api.Controllers;

/// <summary>Cuentas de coordinador de cada universidad (HU-A03). Solo SuperAdmin.</summary>
[ApiController]
[Route("api/v1/admin/coordinadores")]
[Authorize(Roles = "SuperAdmin")]
[Produces("application/json")]
public sealed class AdminCoordinadoresController : ControllerBase
{
    /// <summary>Coordinadores, opcionalmente de una universidad, ordenados por universidad y apellido.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CoordinadorAdminDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CoordinadorAdminDto>> Listar(
        [FromQuery] Guid? universidadId, [FromServices] ListarCoordinadoresHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(universidadId, ct);

    /// <summary>Crea un coordinador con contraseña temporal (se muestra una sola vez; debe cambiarla al ingresar).</summary>
    [HttpPost]
    [ProducesResponseType<CuentaCreadaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CuentaCreadaDto>> Crear(
        CrearCoordinadorRequest request, [FromServices] CrearCoordinadorHandler handler, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await handler.EjecutarAsync(request, ct));

    /// <summary>Activa o desactiva la cuenta. Un coordinador inactivo no puede iniciar sesión.</summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(
        Guid id, CambiarEstadoCoordinadorRequest request, [FromServices] CambiarEstadoCoordinadorHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Genera una nueva contraseña temporal; la anterior deja de funcionar.</summary>
    [HttpPost("{id:guid}/restablecer-password")]
    [ProducesResponseType<CuentaCreadaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CuentaCreadaDto> RestablecerPassword(
        Guid id, [FromServices] RestablecerPasswordCoordinadorHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, ct);
}
