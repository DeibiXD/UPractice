using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Vacantes;

public sealed class CrearVacanteHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<VacanteEmpresaDto> EjecutarAsync(CrearVacanteRequest request, CancellationToken ct)
    {
        var autor = await ReglasVacante.ExigirUsuarioEmpresaAsync(db, actual, ct);
        var seleccion = await ReglasVacante.ValidarSeleccionAsync(db,
            request.CarrerasIds, request.Habilidades, request.Publicar, ct);

        var vacante = new Vacante
        {
            EmpresaId = autor.EmpresaId,
            CreadaPorId = autor.UsuarioId,
            Titulo = request.Titulo.Trim(),
            Descripcion = request.Descripcion.Trim(),
            Modalidad = request.Modalidad,
            Ubicacion = request.Ubicacion.Trim(),
            Cupos = request.Cupos,
            Remunerada = request.Remunerada,
            IndiceMinimo = request.IndiceMinimo,
            FechaCierre = request.FechaCierre,
            Estado = request.Publicar ? EstadoVacante.Publicada : EstadoVacante.Borrador,
            FechaPublicacion = request.Publicar ? fecha.UtcAhora() : null,
        };
        foreach (var carreraId in request.CarrerasIds)
            vacante.Carreras.Add(new VacanteCarrera { VacanteId = vacante.Id, CarreraId = carreraId });
        foreach (var habilidad in request.Habilidades)
            vacante.Habilidades.Add(new VacanteHabilidad
            {
                VacanteId = vacante.Id, HabilidadId = habilidad.HabilidadId, EsRequerida = habilidad.EsRequerida,
            });

        db.Vacantes.Add(vacante);
        await db.SaveChangesAsync(ct);

        // Devolver la precisión realmente persistida: numeric(5,2) y timestamps de PostgreSQL.
        var guardada = await db.Vacantes.AsNoTracking().SingleAsync(v => v.Id == vacante.Id, ct);
        return new VacanteEmpresaDto(
            guardada.Id, guardada.Titulo, guardada.Descripcion, guardada.Modalidad, guardada.Ubicacion,
            guardada.Cupos, guardada.Remunerada, guardada.IndiceMinimo, guardada.FechaCierre,
            guardada.FechaPublicacion, guardada.Estado,
            seleccion.Carreras, seleccion.Habilidades, autor.NombreCompleto,
            0, new Dictionary<EstadoPostulacion, int>());
    }
}
