using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Perfil;
using UPractice.Domain.Entidades;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Curriculums;

internal static class ConsultasCurriculums
{
    /// <summary>El filtro global ya limita Curriculums a los del estudiante actual (S3.2).</summary>
    public static IQueryable<Curriculum> Activos(IAppDbContext db) => db.Curriculums.Where(c => !c.Eliminado);

    public static async Task<Curriculum> PropioActivoAsync(IAppDbContext db, Guid id, CancellationToken ct) =>
        await Activos(db).SingleOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new NoEncontradoException("El currículum no existe.");

    public static CurriculumDto ADto(Curriculum c) => new(c.Id, c.NombreVersion, c.EsPrincipal);

    /// <summary>Nombre de descarga legible: el nombre de la versión sin caracteres inválidos + ".pdf".</summary>
    public static string NombreDescarga(string nombreVersion)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(nombreVersion.Select(ch => invalidos.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return (limpio.Length == 0 ? "curriculum" : limpio) + ".pdf";
    }

    public static async Task<UrlDescargaDto> UrlAsync(IAlmacenamientoArchivos storage, Curriculum c, CancellationToken ct)
    {
        var url = await storage.ObtenerUrlDescargaAsync(c.ArchivoKey, LlavesArchivo.VigenciaDescarga, NombreDescarga(c.NombreVersion), ct);
        return new UrlDescargaDto(url.Url, url.ExpiraEn);
    }
}

/// <summary>GET /estudiantes/me/curriculums (HU-03): versiones activas, la principal primero y luego la más reciente.</summary>
public sealed class ListarMisCurriculumsHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<IReadOnlyList<CurriculumDto>> EjecutarAsync(CancellationToken ct)
    {
        ConsultasPerfil.EstudianteActual(actual);
        return await ConsultasCurriculums.Activos(db).AsNoTracking()
            .OrderByDescending(c => c.EsPrincipal).ThenByDescending(c => c.Id) // Id v7: orden de creación
            .Select(c => new CurriculumDto(c.Id, c.NombreVersion, c.EsPrincipal))
            .ToListAsync(ct);
    }
}

/// <summary>
/// POST /estudiantes/me/curriculums (HU-03). Máximo 5 activos (422 LIMITE_CURRICULUMS). La primera versión queda como principal.
/// Sube el PDF y luego guarda la fila; si guardar falla, borra el objeto para no dejar huérfanos (S3.8).
/// </summary>
public sealed class SubirCurriculumHandler(
    IAppDbContext db, ICurrentUser actual, IAlmacenamientoArchivos storage, IValidator<SubirCurriculumRequest> validador)
{
    public async Task<CurriculumDto> EjecutarAsync(SubirCurriculumRequest request, CancellationToken ct)
    {
        var estudianteId = ConsultasPerfil.EstudianteActual(actual);
        await validador.ValidateAndThrowAsync(request, ct);

        var activos = await ConsultasCurriculums.Activos(db).CountAsync(ct);
        if (activos >= MensajesCurriculums.MaximoActivos)
            throw new ReglaNegocioException(CodigosError.LimiteCurriculums, MensajesCurriculums.Limite);

        var curriculum = new Curriculum
        {
            EstudianteId = estudianteId,
            NombreVersion = request.NombreVersion!.Trim(),
            EsPrincipal = activos == 0,
        };
        curriculum.ArchivoKey = LlavesArchivo.Curriculum(estudianteId, curriculum.Id);

        var archivo = request.Archivo!;
        archivo.Contenido.Position = 0;
        await storage.SubirAsync(curriculum.ArchivoKey, archivo.Contenido, "application/pdf", ct);

        db.Curriculums.Add(curriculum);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await storage.EliminarAsync(curriculum.ArchivoKey, CancellationToken.None);
            throw;
        }
        return ConsultasCurriculums.ADto(curriculum);
    }
}

/// <summary>
/// PATCH /estudiantes/me/curriculums/{id}/principal (HU-03). En dos pasos dentro de una transacción para no violar
/// el índice único parcial "un solo principal por estudiante".
/// </summary>
public sealed class MarcarCurriculumPrincipalHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task EjecutarAsync(Guid id, CancellationToken ct)
    {
        ConsultasPerfil.EstudianteActual(actual);
        var curriculum = await ConsultasCurriculums.PropioActivoAsync(db, id, ct);
        if (curriculum.EsPrincipal) return;

        await using var transaccion = await db.BeginTransactionAsync(ct);
        var anteriores = await ConsultasCurriculums.Activos(db).Where(c => c.EsPrincipal).ToListAsync(ct);
        foreach (var anterior in anteriores) anterior.EsPrincipal = false;
        await db.SaveChangesAsync(ct);

        curriculum.EsPrincipal = true;
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }
}

/// <summary>
/// DELETE /estudiantes/me/curriculums/{id} (HU-03): baja lógica. El PDF se conserva porque puede estar adjunto a
/// postulaciones (la empresa sigue pudiendo descargarlo). Si era la principal, pasa a serlo la versión más reciente.
/// </summary>
public sealed class EliminarCurriculumHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task EjecutarAsync(Guid id, CancellationToken ct)
    {
        ConsultasPerfil.EstudianteActual(actual);
        var curriculum = await ConsultasCurriculums.PropioActivoAsync(db, id, ct);
        var eraPrincipal = curriculum.EsPrincipal;

        await using var transaccion = await db.BeginTransactionAsync(ct);
        curriculum.Eliminado = true;
        curriculum.EsPrincipal = false;
        await db.SaveChangesAsync(ct);

        if (eraPrincipal)
        {
            var siguiente = await ConsultasCurriculums.Activos(db).OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct);
            if (siguiente is not null)
            {
                siguiente.EsPrincipal = true;
                await db.SaveChangesAsync(ct);
            }
        }
        await transaccion.CommitAsync(ct);
    }
}

/// <summary>GET /estudiantes/me/curriculums/{id}/descarga (HU-03): URL firmada de 5 minutos.</summary>
public sealed class DescargarMiCurriculumHandler(IAppDbContext db, ICurrentUser actual, IAlmacenamientoArchivos storage)
{
    public async Task<UrlDescargaDto> EjecutarAsync(Guid id, CancellationToken ct)
    {
        ConsultasPerfil.EstudianteActual(actual);
        var curriculum = await ConsultasCurriculums.PropioActivoAsync(db, id, ct);
        return await ConsultasCurriculums.UrlAsync(storage, curriculum, ct);
    }
}

/// <summary>
/// GET /empresa/postulaciones/{id}/curriculum/descarga (HU-E06 / HU-03): la empresa descarga el CV adjunto a una
/// postulación suya, aunque el estudiante lo haya eliminado después. Postulación de otra empresa → 404 (filtro global).
/// </summary>
public sealed class DescargarCurriculumPostulacionHandler(IAppDbContext db, IAlmacenamientoArchivos storage)
{
    public async Task<UrlDescargaDto> EjecutarAsync(Guid postulacionId, CancellationToken ct)
    {
        var curriculum = await db.Postulaciones.AsNoTracking()
            .Where(p => p.Id == postulacionId)
            .Select(p => p.Curriculum)
            .SingleOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("La postulación no existe.");
        return await ConsultasCurriculums.UrlAsync(storage, curriculum, ct);
    }
}
