using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Empresa.Habilitaciones;
using UPractice.Domain.Excepciones;

namespace UPractice.Tests.Infraestructura;

/// <summary>
/// Endpoints mínimos que SOLO existen en las pruebas: leen vacantes y postulaciones a través del filtro global,
/// para comprobar el aislamiento por HTTP antes de que existan los endpoints reales de las HU.
/// </summary>
[ApiController]
[Route("api/v1/pruebas")]
public sealed class PruebasController(IAppDbContext db) : ControllerBase
{
    [HttpGet("vacantes")]
    public Task<List<Guid>> Vacantes() => db.Vacantes.Select(v => v.Id).ToListAsync();

    [HttpGet("vacantes/{id:guid}")]
    public async Task<object> Vacante(Guid id) =>
        await db.Vacantes.Where(v => v.Id == id).Select(v => new { v.Id, v.Titulo }).FirstOrDefaultAsync()
        ?? throw new NoEncontradoException("La vacante no existe.");

    [HttpGet("postulaciones")]
    public Task<List<Guid>> Postulaciones() => db.Postulaciones.Select(p => p.Id).ToListAsync();

    [HttpPost("publicar")]
    public async Task<IActionResult> ValidarPublicacion(
        [FromServices] IEmpresaPublicacionGuard guard,
        CancellationToken ct)
    {
        await guard.ValidarPuedePublicarAsync(ct);
        return NoContent();
    }
}
