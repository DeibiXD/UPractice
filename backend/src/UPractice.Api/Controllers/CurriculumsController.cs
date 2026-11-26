using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Curriculums;

namespace UPractice.Api.Controllers;

/// <summary>Versiones del CV del estudiante (HU-03). Los PDF van al almacenamiento (T-08); la BD guarda solo la llave.</summary>
[ApiController]
[Route("api/v1/estudiantes/me/curriculums")]
[Authorize(Roles = "Estudiante")]
[Produces("application/json")]
public sealed class CurriculumsController : ControllerBase
{
    /// <summary>Versiones activas: la principal primero y luego la más reciente.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CurriculumDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CurriculumDto>> Listar([FromServices] ListarMisCurriculumsHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(ct);

    /// <summary>Sube una versión: PDF de hasta 5 MB (multipart: archivo y nombreVersion). Máximo 5 activas.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    [ProducesResponseType<CurriculumDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CurriculumDto>> Subir(
        [FromForm] SubirCurriculumFormulario formulario, [FromServices] SubirCurriculumHandler handler, CancellationToken ct)
    {
        // Se copia a memoria (≤ 5 MB) para revisar la firma %PDF y subirlo desde el inicio. Más grande: no se lee;
        // el validador lo rechaza por tamaño.
        ArchivoSubido? archivo = null;
        if (formulario.Archivo is { } f)
        {
            var contenido = new MemoryStream();
            if (f.Length <= MensajesCurriculums.TamanoMaximo)
            {
                await f.CopyToAsync(contenido, ct);
                contenido.Position = 0;
            }
            archivo = new ArchivoSubido(f.FileName, f.ContentType, f.Length, contenido);
        }

        await using var _ = archivo?.Contenido;
        var creado = await handler.EjecutarAsync(new SubirCurriculumRequest(formulario.NombreVersion, archivo), ct);
        return StatusCode(StatusCodes.Status201Created, creado);
    }

    /// <summary>Marca la versión como principal (se preselecciona al postular).</summary>
    [HttpPatch("{id:guid}/principal")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarPrincipal(Guid id, [FromServices] MarcarCurriculumPrincipalHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Baja lógica: deja de aparecer, pero las empresas donde se adjuntó conservan el archivo.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, [FromServices] EliminarCurriculumHandler handler, CancellationToken ct)
    {
        await handler.EjecutarAsync(id, ct);
        return NoContent();
    }

    /// <summary>URL firmada para descargar el PDF, válida 5 minutos.</summary>
    [HttpGet("{id:guid}/descarga")]
    [ProducesResponseType<UrlDescargaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UrlDescargaDto> Descargar(Guid id, [FromServices] DescargarMiCurriculumHandler handler, CancellationToken ct)
        => handler.EjecutarAsync(id, ct);
}

/// <summary>Formulario multipart de la subida. Solo existe en la API; Application recibe un ArchivoSubido.</summary>
public sealed class SubirCurriculumFormulario
{
    public IFormFile? Archivo { get; set; }
    public string? NombreVersion { get; set; }
}
