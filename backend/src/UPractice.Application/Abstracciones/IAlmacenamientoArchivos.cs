namespace UPractice.Application.Abstracciones;

/// <summary>
/// Almacenamiento de objetos para los PDF (S3.8 / D-09). La BD guarda solo la llave (<c>ArchivoKey</c>),
/// nunca el archivo ni la URL. Implementaciones en Infrastructure: <c>LocalStorage</c> (desarrollo y Docker)
/// y <c>SupabaseStorage</c> (producción), elegidas con <c>Storage:Proveedor</c>.
/// </summary>
public interface IAlmacenamientoArchivos
{
    /// <summary>Guarda (o reemplaza) el objeto en la llave indicada. La llave la genera la API con <see cref="LlavesArchivo"/>.</summary>
    Task SubirAsync(string key, Stream contenido, string contentType, CancellationToken ct);

    /// <summary>URL temporal para descargar el objeto. Cuando vence deja de funcionar aunque alguien la haya copiado.</summary>
    Task<UrlDescarga> ObtenerUrlDescargaAsync(string key, TimeSpan vigencia, string nombreDescarga, CancellationToken ct);

    /// <summary>Borra el objeto. No falla si ya no existe (sirve para limpiar si falla guardar en la BD).</summary>
    Task EliminarAsync(string key, CancellationToken ct);
}

/// <summary>URL firmada y su vencimiento en UTC. Se expone al frontend como <c>UrlDescargaDto</c>.</summary>
public sealed record UrlDescarga(string Url, DateTime ExpiraEn);

/// <summary>Formato de las llaves (S3.8). Se usan ids, nunca el nombre original, para que nadie adivine rutas.</summary>
public static class LlavesArchivo
{
    /// <summary>Vigencia de las URL de descarga (S3.8: 5 minutos).</summary>
    public static readonly TimeSpan VigenciaDescarga = TimeSpan.FromMinutes(5);

    public static string Curriculum(Guid estudianteId, Guid curriculumId) =>
        $"curriculums/{estudianteId}/{curriculumId}.pdf";

    public static string DocumentoPractica(Guid empresaId, Guid practicaId, Guid documentoId) =>
        $"practicas/{empresaId}/{practicaId}/{documentoId}.pdf";
}
