using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;
using UPractice.Domain.Reglas;

namespace UPractice.Application.Features.Postulaciones;

/// <summary>GET /postulaciones (HU-06): el panel del estudiante. El filtro global deja solo sus postulaciones.</summary>
public sealed class ListarMisPostulacionesHandler(IAppDbContext db)
{
    public Task<List<MiPostulacionDto>> EjecutarAsync(CancellationToken ct) => ConsultasPostulacion.ListarMiasAsync(db, ct);
}

/// <summary>GET /postulaciones/{id} (HU-06): detalle con historial. La de otro estudiante responde 404.</summary>
public sealed class ObtenerMiPostulacionHandler(IAppDbContext db)
{
    public Task<MiPostulacionDetalleDto> EjecutarAsync(Guid id, CancellationToken ct) => ConsultasPostulacion.DetalleMioAsync(db, id, ct);
}

/// <summary>POST /postulaciones/{id}/retirar (HU-06). Solo desde Enviada, EnRevision o Entrevista.</summary>
public sealed class RetirarPostulacionHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<MiPostulacionDetalleDto> EjecutarAsync(Guid id, CancellationToken ct)
    {
        var estudianteId = ReglasPostulacion.ExigirUsuario(actual);
        var postulacion = await db.Postulaciones.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NoEncontradoException("La postulación no existe.");

        TransicionesPostulacion.Validar(postulacion.Estado, EstadoPostulacion.Retirada, ActorPostulacion.Estudiante);

        var anterior = postulacion.Estado;
        postulacion.Estado = EstadoPostulacion.Retirada;
        db.HistorialPostulacion.Add(ReglasPostulacion.NuevoHistorial(postulacion, anterior, EstadoPostulacion.Retirada, estudianteId));
        await db.SaveChangesAsync(ct);

        return await ConsultasPostulacion.DetalleMioAsync(db, id, ct);
    }
}

/// <summary>
/// POST /postulaciones/{id}/reactivar (HU-06, D-20). Retirada → Enviada, con las mismas validaciones que al postularse
/// y la posibilidad de elegir otro CV. Se recalcula CumpleRequisitos con el índice actual.
/// </summary>
public sealed class ReactivarPostulacionHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<MiPostulacionDetalleDto> EjecutarAsync(Guid id, ReactivarPostulacionRequest request, CancellationToken ct)
    {
        var estudianteId = ReglasPostulacion.ExigirUsuario(actual);
        var postulacion = await db.Postulaciones.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NoEncontradoException("La postulación no existe.");

        TransicionesPostulacion.Validar(postulacion.Estado, EstadoPostulacion.Enviada, ActorPostulacion.Estudiante);

        var vacante = await ReglasPostulacion.ExigirVacanteDisponibleAsync(db, postulacion.VacanteId, ct);
        await ReglasPostulacion.ExigirSinPracticaAceptadaAsync(db, ct);
        var perfil = await ReglasPostulacion.ExigirPerfilElegibleAsync(db, postulacion.VacanteId, ct);
        await ReglasPostulacion.ExigirCurriculumAsync(db, request.CurriculumId, ct);

        var anterior = postulacion.Estado;
        postulacion.Estado = EstadoPostulacion.Enviada;
        postulacion.CurriculumId = request.CurriculumId;
        postulacion.CumpleRequisitos = ReglasPostulacion.CumpleIndice(vacante.IndiceMinimo, perfil.IndiceAcademico);
        db.HistorialPostulacion.Add(ReglasPostulacion.NuevoHistorial(postulacion, anterior, EstadoPostulacion.Enviada, estudianteId, "Postulación reactivada."));
        await db.SaveChangesAsync(ct);

        return await ConsultasPostulacion.DetalleMioAsync(db, id, ct);
    }
}

/// <summary>
/// PUT /postulaciones/{id}/oferta (HU-06). Oferta → Aceptada o Declinada. Al aceptar, el resto de las postulaciones
/// activas del estudiante se retiran solas, porque solo puede hacer una práctica.
/// </summary>
public sealed class ResponderOfertaHandler(IAppDbContext db, ICurrentUser actual)
{
    public async Task<MiPostulacionDetalleDto> EjecutarAsync(Guid id, ResponderOfertaRequest request, CancellationToken ct)
    {
        var estudianteId = ReglasPostulacion.ExigirUsuario(actual);
        var postulacion = await db.Postulaciones.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NoEncontradoException("La postulación no existe.");

        var aceptar = request.Aceptar == true;
        var destino = aceptar ? EstadoPostulacion.Aceptada : EstadoPostulacion.Declinada;
        TransicionesPostulacion.Validar(postulacion.Estado, destino, ActorPostulacion.Estudiante);

        if (aceptar)
        {
            await ReglasPostulacion.ExigirSinPracticaAceptadaAsync(db, ct);

            // Son pocas postulaciones por estudiante: se filtran en memoria con el conjunto "Activos" de la máquina de estados.
            var otras = (await db.Postulaciones.Where(p => p.Id != id).ToListAsync(ct))
                .Where(p => TransicionesPostulacion.Activos.Contains(p.Estado));
            foreach (var otra in otras)
            {
                var estadoOtra = otra.Estado;
                otra.Estado = EstadoPostulacion.Retirada;   // retiro automático: no pasa por la tabla de transiciones del estudiante
                db.HistorialPostulacion.Add(ReglasPostulacion.NuevoHistorial(
                    otra, estadoOtra, EstadoPostulacion.Retirada, estudianteId, "Retirada automáticamente al aceptar otra oferta."));
            }
        }

        var anterior = postulacion.Estado;
        postulacion.Estado = destino;
        db.HistorialPostulacion.Add(ReglasPostulacion.NuevoHistorial(postulacion, anterior, destino, estudianteId));
        await db.SaveChangesAsync(ct);

        return await ConsultasPostulacion.DetalleMioAsync(db, id, ct);
    }
}
