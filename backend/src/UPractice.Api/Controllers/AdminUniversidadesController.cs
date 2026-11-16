using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Features.Admin.Universidades.DTOs;
using UPractice.Application.Features.Admin.Universidades.Services;

namespace UPractice.Api.Controllers;

/// <summary>Administración del catálogo de universidades.</summary>
[ApiController]
[Route("api/v1/admin/universidades")]
[Authorize(Roles = "SuperAdmin")]
[Produces("application/json")]
public sealed class AdminUniversidadesController : ControllerBase
{
    /// <summary>Lista todas las universidades, activas e inactivas.</summary>
    [HttpGet]
    [EndpointSummary("Lista universidades para administración")]
    [ProducesResponseType<IReadOnlyList<UniversidadAdminResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public Task<IReadOnlyList<UniversidadAdminResponseDto>> Listar(
        [FromServices] ListarUniversidadesAdminHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(ct);

    /// <summary>Registra una universidad activa.</summary>
    [HttpPost]
    [EndpointSummary("Registra una universidad")]
    [ProducesResponseType<UniversidadAdminResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UniversidadAdminResponseDto>> Crear(
        CrearUniversidadRequestDto request,
        [FromServices] CrearUniversidadHandler handler,
        CancellationToken ct)
    {
        var respuesta = await handler.EjecutarAsync(request, ct);
        return Created("/api/v1/admin/universidades", respuesta);
    }

    /// <summary>Actualiza el nombre, las siglas y el dominio de correo de una universidad.</summary>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza una universidad")]
    [ProducesResponseType<UniversidadAdminResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<UniversidadAdminResponseDto> Actualizar(
        Guid id,
        ActualizarUniversidadRequestDto request,
        [FromServices] ActualizarUniversidadHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(id, request, ct);

    /// <summary>Activa o desactiva una universidad.</summary>
    [HttpPatch("{id:guid}/estado")]
    [EndpointSummary("Cambia el estado activo de una universidad")]
    [ProducesResponseType<UniversidadAdminResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UniversidadAdminResponseDto> CambiarEstado(
        Guid id,
        CambiarEstadoUniversidadRequestDto request,
        [FromServices] CambiarEstadoUniversidadHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(id, request, ct);
}
