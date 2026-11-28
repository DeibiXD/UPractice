namespace UPractice.Application.Features.Curriculums;

/// <summary>S5.6 <c>CurriculumDto</c>.</summary>
public sealed record CurriculumDto(Guid Id, string NombreVersion, bool EsPrincipal);

/// <summary>
/// Archivo recibido por la API, sin tipos de ASP.NET (Clean Architecture): el controlador lo arma desde el IFormFile.
/// <c>Contenido</c> debe permitir Seek para revisar la firma %PDF y luego subirlo desde el inicio.
/// </summary>
public sealed record ArchivoSubido(string NombreArchivo, string ContentType, long Tamano, Stream Contenido);

/// <summary>POST /estudiantes/me/curriculums (multipart: archivo, nombreVersion).</summary>
public sealed record SubirCurriculumRequest(string? NombreVersion, ArchivoSubido? Archivo);

/// <summary>Textos idénticos a docs/mensajes-validacion.md y al esquema Zod del frontend.</summary>
public static class MensajesCurriculums
{
    public const string NombreVersion = "El nombre de la versión debe tener entre 3 y 60 caracteres.";
    public const string Archivo = "Suba un archivo PDF de máximo 5 MB.";
    public const string Limite = "Puede tener como máximo 5 versiones activas de su CV.";
    public const long TamanoMaximo = 5 * 1024 * 1024;
    public const int MaximoActivos = 5;
}
