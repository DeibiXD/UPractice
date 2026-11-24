using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Postulaciones;

/// <summary>
/// POST /vacantes/{vacanteId}/postulaciones (HU-05). El estudiante sale de ICurrentUser; el EmpresaId, de la vacante.
/// </summary>
public sealed class PostularseHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<MiPostulacionDetalleDto> EjecutarAsync(Guid vacanteId, CrearPostulacionRequest request, CancellationToken ct)
    {
        var estudianteId = ReglasPostulacion.ExigirUsuario(actual);

        // Primero el duplicado: si ya hay una postulación (incluso retirada) la vacante existe y no se revela nada ajeno.
        if (await db.Postulaciones.AnyAsync(p => p.VacanteId == vacanteId, ct))
            throw new ConflictoException(CodigosError.PostulacionDuplicada,
                "Ya se postuló a esta vacante. Si la retiró, puede reactivarla desde su panel.");

        var vacante = await ReglasPostulacion.ExigirVacanteDisponibleAsync(db, fecha, vacanteId, ct);
        await ReglasPostulacion.ExigirSinPracticaAceptadaAsync(db, ct);
        var perfil = await ReglasPostulacion.ExigirPerfilElegibleAsync(db, vacanteId, ct);
        await ReglasPostulacion.ExigirCurriculumAsync(db, request.CurriculumId, ct);

        var ahora = fecha.UtcAhora();
        var postulacion = new Postulacion
        {
            EmpresaId = vacante.EmpresaId,
            VacanteId = vacanteId,
            EstudianteId = estudianteId,
            CurriculumId = request.CurriculumId,
            Estado = EstadoPostulacion.Enviada,
            CumpleRequisitos = ReglasPostulacion.CumpleIndice(vacante.IndiceMinimo, perfil.IndiceAcademico),
            FechaPostulacion = ahora,
        };
        postulacion.Historial.Add(ReglasPostulacion.NuevoHistorial(postulacion, null, EstadoPostulacion.Enviada, estudianteId, ahora));
        db.Postulaciones.Add(postulacion);
        await db.SaveChangesAsync(ct);

        return await ConsultasPostulacion.DetalleMioAsync(db, fecha.Hoy(), postulacion.Id, ct);
    }
}
