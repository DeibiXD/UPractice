using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Features.Catalogos;

namespace UPractice.Api.Controllers;

/// <summary>Catálogos públicos usados por los formularios de registro.</summary>
[ApiController]
[Route("api/v1/catalogos")]
[Produces("application/json")]
public sealed class CatalogosController : ControllerBase
{
    /// <summary>Devuelve únicamente las universidades activas.</summary>
    [HttpGet("universidades")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<UniversidadDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UniversidadDto>> Universidades(
        [FromServices] ListarUniversidadesHandler handler,
        CancellationToken ct) => handler.EjecutarAsync(ct);
}
