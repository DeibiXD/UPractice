using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;
using UPractice.Domain.Reglas;

namespace UPractice.Application.Features.Postulaciones;

/// <summary>GET /empresa/vacantes/{vacanteId}/postulantes (HU-E05). Una vacante de otra empresa responde 404.</summary>
public sealed class ListarPostulantesHandler(IAppDbContext db)
{
    public Task<List<PostulanteResumenDto>> EjecutarAsync(Guid vacanteId, CancellationToken ct) =>
        ConsultasPostulacion.ListarPostulantesAsync(db, vacanteId, ct);
}

/// <summary>
/// GET /empresa/postulaciones/{id} (HU-E06). El perfil y el CV solo se leen por la postulación a esta empresa (filtros globales);
/// el índice académico solo si el estudiante lo autorizó.
/// </summary>
public sealed class ObtenerPostulanteHandler(IAppDbContext db)
{
    public Task<PostulanteDetalleDto> EjecutarAsync(Guid id, CancellationToken ct) =>
        ConsultasPostulacion.DetallePostulanteAsync(db, id, ct);
}

/// <summary>
/// PUT /empresa/postulaciones/{id}/estado (HU-E07). La máquina de estados decide qué saltos puede hacer la empresa;
/// la oferta además necesita un cupo libre de la vacante.
/// </summary>
public sealed class CambiarEstadoPostulacionHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<PostulanteDetalleDto> EjecutarAsync(Guid id, CambiarEstadoPostulacionRequest request, CancellationToken ct)
    {
        var usuarioId = ReglasPostulacion.ExigirUsuario(actual);
        var postulacion = await db.Postulaciones.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NoEncontradoException("La postulación no existe.");

        TransicionesPostulacion.Validar(postulacion.Estado, request.NuevoEstado, ActorPostulacion.Empresa);

        if (request.NuevoEstado == EstadoPostulacion.Oferta)
            await ExigirCupoLibreAsync(postulacion.VacanteId, ct);

        var anterior = postulacion.Estado;
        postulacion.Estado = request.NuevoEstado;
        var comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim();
        db.HistorialPostulacion.Add(ReglasPostulacion.NuevoHistorial(postulacion, anterior, request.NuevoEstado, usuarioId, comentario));
        await db.SaveChangesAsync(ct);

        return await ConsultasPostulacion.DetallePostulanteAsync(db, id, ct);
    }

    /// <summary>
    /// Cada oferta pendiente y cada práctica aceptada ocupa un cupo. Se cuenta aquí (lado empresa) porque el estudiante,
    /// por el filtro global, solo ve sus propias postulaciones y no podría contar las de los demás.
    /// </summary>
    private async Task ExigirCupoLibreAsync(Guid vacanteId, CancellationToken ct)
    {
        var cupos = await db.Vacantes.Where(v => v.Id == vacanteId).Select(v => v.Cupos).FirstAsync(ct);
        var ocupados = await db.Postulaciones.CountAsync(
            p => p.VacanteId == vacanteId && (p.Estado == EstadoPostulacion.Oferta || p.Estado == EstadoPostulacion.Aceptada), ct);

        if (ocupados >= cupos)
            throw new ReglaNegocioException(CodigosError.SinCupos, "La vacante ya no tiene cupos disponibles para nuevas ofertas.");
    }
}
