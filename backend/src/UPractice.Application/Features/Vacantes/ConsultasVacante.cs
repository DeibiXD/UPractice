using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Vacantes;

internal static class ConsultasVacante
{
    public static IQueryable<Vacante> FiltrarEstado(IQueryable<Vacante> consulta, EstadoVacante estado, DateOnly hoy) =>
        estado switch
        {
            EstadoVacante.Publicada => consulta.Where(v => v.Estado == EstadoVacante.Publicada && v.FechaCierre >= hoy),
            EstadoVacante.Cerrada => consulta.Where(v => v.Estado == EstadoVacante.Cerrada
                || (v.Estado == EstadoVacante.Publicada && v.FechaCierre < hoy)),
            _ => consulta.Where(v => v.Estado == estado),
        };

    public static async Task<VacanteEmpresaDto> ObtenerAsync(IAppDbContext db, Guid id, DateOnly hoy, CancellationToken ct) =>
        (await MaterializarAsync(db, db.Vacantes.Where(v => v.Id == id), hoy, ct)).SingleOrDefault()
        ?? throw new NoEncontradoException("La vacante no existe.");

    public static async Task<IReadOnlyList<VacanteEmpresaDto>> MaterializarAsync(
        IAppDbContext db, IQueryable<Vacante> consulta, DateOnly hoy, CancellationToken ct)
    {
        var filas = await consulta.AsNoTracking().Select(v => new
        {
            v.Id, v.Titulo, v.Descripcion, v.Modalidad, v.Ubicacion, v.Cupos, v.Remunerada,
            v.IndiceMinimo, v.FechaCierre, v.FechaPublicacion, v.Estado,
            v.CreadaPor.Nombre1, v.CreadaPor.Nombre2, v.CreadaPor.Apellido1, v.CreadaPor.Apellido2,
        }).ToListAsync(ct);
        if (filas.Count == 0) return [];

        // Consultas en lotes de la página: sin N+1 ni productos cartesianos entre colecciones.
        var ids = filas.Select(v => v.Id).ToArray();
        var carreras = await db.VacanteCarreras.AsNoTracking().Where(vc => ids.Contains(vc.VacanteId))
            .OrderBy(vc => vc.Carrera.Nombre).ThenBy(vc => vc.CarreraId)
            .Select(vc => new
            {
                vc.VacanteId, vc.CarreraId, vc.Carrera.Nombre,
                Facultad = vc.Carrera.Facultad.Nombre, vc.Carrera.Facultad.UniversidadId,
            }).ToListAsync(ct);
        var habilidades = await db.VacanteHabilidades.AsNoTracking().Where(vh => ids.Contains(vh.VacanteId))
            .OrderBy(vh => vh.Habilidad.Nombre).ThenBy(vh => vh.HabilidadId)
            .Select(vh => new { vh.VacanteId, vh.HabilidadId, vh.Habilidad.Nombre, vh.EsRequerida }).ToListAsync(ct);
        var conteos = await db.Postulaciones.AsNoTracking().Where(p => ids.Contains(p.VacanteId))
            .GroupBy(p => new { p.VacanteId, p.Estado })
            .Select(g => new { g.Key.VacanteId, g.Key.Estado, Total = g.Count() }).ToListAsync(ct);

        var carrerasPorVacante = carreras.ToLookup(c => c.VacanteId);
        var habilidadesPorVacante = habilidades.ToLookup(h => h.VacanteId);
        var conteosPorVacante = conteos.ToLookup(c => c.VacanteId);
        return filas.Select(v => new VacanteEmpresaDto(
            v.Id, v.Titulo, v.Descripcion, v.Modalidad, v.Ubicacion, v.Cupos, v.Remunerada,
            v.IndiceMinimo, v.FechaCierre, v.FechaPublicacion,
            ReglasVacante.EstadoEfectivo(v.Estado, v.FechaCierre, hoy),
            carrerasPorVacante[v.Id].Select(c => new CarreraDto(c.CarreraId, c.Nombre, c.Facultad, c.UniversidadId)).ToArray(),
            habilidadesPorVacante[v.Id].Select(h => new HabilidadVacanteDto(h.HabilidadId, h.Nombre, h.EsRequerida)).ToArray(),
            Usuario.ArmarNombreCompleto(v.Nombre1, v.Nombre2, v.Apellido1, v.Apellido2),
            conteosPorVacante[v.Id].Sum(c => c.Total),
            conteosPorVacante[v.Id].ToDictionary(c => c.Estado, c => c.Total))).ToArray();
    }
}
