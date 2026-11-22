using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UPractice.Api.Errores;
using UPractice.Infrastructure.Almacenamiento;

namespace UPractice.Api.Controllers;

/// <summary>
/// Descarga de archivos del proveedor LOCAL (desarrollo y Docker, S3.8). La URL firmada es la credencial:
/// no lleva token porque el navegador la abre directamente, igual que una URL firmada de Supabase.
/// Con Storage:Proveedor = Supabase este endpoint siempre responde 404.
/// </summary>
[ApiController]
[Route("api/v1/archivos")]
public sealed class ArchivosLocalesController : ControllerBase
{
    [HttpGet("local")]
    [AllowAnonymous]
    [EndpointSummary("Descarga un archivo con una URL firmada del almacenamiento local")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public IActionResult Descargar(
        [FromQuery] string? key, [FromQuery] long exp, [FromQuery] string? nombre, [FromQuery] string? sig,
        [FromServices] IServiceProvider servicios)
    {
        var archivo = servicios.GetService<LocalStorage>()?.AbrirSiFirmaValida(key, exp, nombre, sig);
        if (archivo is null)
        {
            // Firma inválida, enlace vencido o archivo inexistente: misma respuesta para no dar pistas.
            var problema = Problemas.NoEncontrado(HttpContext, "El enlace de descarga no es válido o ya venció.");
            return new ObjectResult(problema) { StatusCode = problema.Status, ContentTypes = { "application/problem+json" } };
        }
        return File(archivo, "application/pdf", nombre);
    }
}
