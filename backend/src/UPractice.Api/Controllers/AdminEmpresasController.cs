using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Admin.Empresas;

namespace UPractice.Api.Controllers;

/// <summary>Supervisión y suspensión de empresas (HU-A04). Solo SuperAdmin; no ve datos de negocio (D-14).</summary>
[ApiController]
[Route("api/v1/admin/empresas")]
[Authorize(Roles = "SuperAdmin")]
[Produces("application/json")]
public sealed class AdminEmpresasController : ControllerBase
{
    /// <summary>Empresas con RTN, sector, fecha de registro, estado y habilitaciones por universidad. Busca por nombre, razón social o RTN.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaResultado<EmpresaAdminDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PaginaResultado<EmpresaAdminDto>> Listar(
        [FromQuery] ListarEmpresasAdminRequest request, [FromServices] ListarEmpresasAdminHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(request, ct);

    /// <summary>Suspende (con motivo obligatorio) o reactiva una empresa.</summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(
        Guid id, CambiarEstadoEmpresaRequest request, [FromServices] CambiarEstadoEmpresaHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(id, request, ct);
        return NoContent();
    }
}
