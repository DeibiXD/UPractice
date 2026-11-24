using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Application.Features.Auth;
using UPractice.Domain.Entidades;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Vacantes;

internal static class ConsultasExploracionVacantes
{
    public static async Task<IReadOnlyList<VacanteResumenDto>> ListarAsync(IAppDbContext db,
        IQueryable<Vacante> consulta, ReglasExploracionVacantes.Estudiante estudiante, CancellationToken ct)
    {
        var filas = await consulta.AsNoTracking().Select(v => new
        {
            v.Id, v.Titulo, v.EmpresaId, Empresa = v.Empresa.NombreComercial,
            v.Modalidad, v.Ubicacion, v.Remunerada, v.FechaPublicacion, v.FechaCierre, v.IndiceMinimo,
            YaPostulado = db.Postulaciones.Any(p => p.VacanteId == v.Id && p.EstudianteId == estudiante.UsuarioId),
        }).ToListAsync(ct);
        if (filas.Count == 0) return [];

        // Consultas acotadas a los IDs de esta página, sin cargar descripciones ni hacer N+1.
        var ids = filas.Select(v => v.Id).ToArray();
        var carreras = await db.VacanteCarreras.AsNoTracking().Where(vc => ids.Contains(vc.VacanteId))
            .OrderBy(vc => vc.Carrera.Nombre).ThenBy(vc => vc.CarreraId)
            .Select(vc => new { vc.VacanteId, vc.CarreraId, vc.Carrera.Nombre }).ToListAsync(ct);
        var requeridas = await db.VacanteHabilidades.AsNoTracking()
            .Where(vh => ids.Contains(vh.VacanteId) && vh.EsRequerida)
            .Select(vh => new { vh.VacanteId, vh.HabilidadId }).ToListAsync(ct);
        var habilidadesPropias = await HabilidadesPropiasAsync(db, estudiante.UsuarioId, ct);
        var carrerasPorVacante = carreras.ToLookup(c => c.VacanteId);
        var requeridasPorVacante = requeridas.ToLookup(h => h.VacanteId);

        return filas.Select(v => new VacanteResumenDto(
            v.Id, v.Titulo, new Referencia(v.EmpresaId, v.Empresa), v.Modalidad, v.Ubicacion, v.Remunerada,
            carrerasPorVacante[v.Id].Select(c => c.Nombre).ToArray(),
            v.FechaPublicacion!.Value, v.FechaCierre,
            CumpleRequisitos(estudiante, v.IndiceMinimo,
                carrerasPorVacante[v.Id].Select(c => c.CarreraId),
                requeridasPorVacante[v.Id].Select(h => h.HabilidadId), habilidadesPropias),
            v.YaPostulado)).ToArray();
    }

    public static async Task<VacanteDetalleDto> DetalleAsync(IAppDbContext db, IQueryable<Vacante> visibles,
        Guid id, ReglasExploracionVacantes.Estudiante estudiante, CancellationToken ct)
    {
        var fila = await visibles.AsNoTracking().Where(v => v.Id == id).Select(v => new
        {
            v.Id, v.Titulo, v.EmpresaId, Empresa = v.Empresa.NombreComercial,
            v.Modalidad, v.Ubicacion, v.Remunerada, v.FechaPublicacion, v.FechaCierre,
            v.Descripcion, v.Cupos, v.IndiceMinimo,
            YaPostulado = db.Postulaciones.Any(p => p.VacanteId == v.Id && p.EstudianteId == estudiante.UsuarioId),
        }).SingleOrDefaultAsync(ct) ?? throw new NoEncontradoException("La vacante no existe.");

        var carreras = await db.VacanteCarreras.AsNoTracking().Where(vc => vc.VacanteId == id)
            .OrderBy(vc => vc.Carrera.Nombre).ThenBy(vc => vc.CarreraId)
            .Select(vc => new CarreraDto(vc.CarreraId, vc.Carrera.Nombre, vc.Carrera.Facultad.Nombre,
                vc.Carrera.Facultad.UniversidadId)).ToListAsync(ct);
        var habilidades = await db.VacanteHabilidades.AsNoTracking().Where(vh => vh.VacanteId == id)
            .OrderBy(vh => vh.Habilidad.Nombre).ThenBy(vh => vh.HabilidadId)
            .Select(vh => new HabilidadVacanteDto(vh.HabilidadId, vh.Habilidad.Nombre, vh.EsRequerida)).ToListAsync(ct);
        var habilidadesPropias = await HabilidadesPropiasAsync(db, estudiante.UsuarioId, ct);
        return new VacanteDetalleDto(
            fila.Id, fila.Titulo, new Referencia(fila.EmpresaId, fila.Empresa), fila.Modalidad, fila.Ubicacion,
            fila.Remunerada, carreras.Select(c => c.Nombre).ToArray(), fila.FechaPublicacion!.Value, fila.FechaCierre,
            CumpleRequisitos(estudiante, fila.IndiceMinimo, carreras.Select(c => c.Id),
                habilidades.Where(h => h.EsRequerida).Select(h => h.Id), habilidadesPropias),
            fila.YaPostulado, fila.Descripcion, fila.Cupos, carreras, fila.IndiceMinimo, habilidades);
    }

    private static async Task<HashSet<Guid>> HabilidadesPropiasAsync(IAppDbContext db, Guid usuarioId, CancellationToken ct) =>
        (await db.EstudianteHabilidades.AsNoTracking()
            // Esta tabla no tiene filtro global: restringir siempre al estudiante autenticado.
            .Where(h => h.EstudianteId == usuarioId).Select(h => h.HabilidadId).ToListAsync(ct)).ToHashSet();

    private static bool CumpleRequisitos(ReglasExploracionVacantes.Estudiante estudiante, decimal? indiceMinimo,
        IEnumerable<Guid> carreras, IEnumerable<Guid> requeridas, HashSet<Guid> habilidadesPropias) =>
        estudiante.CarreraId is { } carreraId && estudiante.IndiceAcademico is { } indice
        && carreras.Contains(carreraId) && (indiceMinimo is null || indice >= indiceMinimo)
        && requeridas.All(habilidadesPropias.Contains);
}
