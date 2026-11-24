using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;
using UPractice.Domain.Reglas;

namespace UPractice.Application.Features.Vacantes;

public sealed class ListarVacantesHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<PaginaResultado<VacanteEmpresaDto>> EjecutarAsync(ListarVacantesRequest request, CancellationToken ct)
    {
        await ReglasVacante.ExigirUsuarioEmpresaAsync(db, actual, ct);
        var hoy = fecha.Hoy();
        IQueryable<Vacante> consulta = db.Vacantes;
        if (request.Estado is { } estado) consulta = ConsultasVacante.FiltrarEstado(consulta, estado, hoy);
        var total = await consulta.CountAsync(ct);
        var totalPaginas = (int)Math.Ceiling(total / (double)request.TamanoPagina);
        var desplazamiento = ((long)request.Pagina - 1) * request.TamanoPagina;
        IReadOnlyList<VacanteEmpresaDto> items = desplazamiento >= total ? [] :
            await ConsultasVacante.MaterializarAsync(db,
                consulta.OrderByDescending(v => v.Id).Skip((int)desplazamiento).Take(request.TamanoPagina), hoy, ct);
        return new PaginaResultado<VacanteEmpresaDto>(items, request.Pagina, request.TamanoPagina, total, totalPaginas);
    }
}

public sealed class ObtenerVacanteHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<VacanteEmpresaDto> EjecutarAsync(Guid id, CancellationToken ct)
    {
        await ReglasVacante.ExigirUsuarioEmpresaAsync(db, actual, ct);
        return await ConsultasVacante.ObtenerAsync(db, id, fecha.Hoy(), ct);
    }
}

public sealed class ActualizarVacanteHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<VacanteEmpresaDto> EjecutarAsync(Guid id, ActualizarVacanteRequest request, CancellationToken ct)
    {
        await ReglasVacante.ExigirUsuarioEmpresaAsync(db, actual, ct);
        var vacante = await db.Vacantes.SingleOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NoEncontradoException("La vacante no existe.");
        if (!TransicionesVacante.SePuedeEditar(ReglasVacante.EstadoEfectivo(vacante.Estado, vacante.FechaCierre, fecha.Hoy())))
            throw new ReglaNegocioException(CodigosError.TransicionInvalida,
                "Solo se pueden editar vacantes en Borrador o Pausada.");

        await ReglasVacante.ValidarSeleccionAsync(db, request.CarrerasIds, request.Habilidades, false, ct);
        await db.VacanteCarreras.Where(c => c.VacanteId == id).LoadAsync(ct);
        await db.VacanteHabilidades.Where(h => h.VacanteId == id).LoadAsync(ct);
        vacante.Titulo = request.Titulo.Trim();
        vacante.Descripcion = request.Descripcion.Trim();
        vacante.Modalidad = request.Modalidad;
        vacante.Ubicacion = request.Ubicacion.Trim();
        vacante.Cupos = request.Cupos;
        vacante.Remunerada = request.Remunerada;
        vacante.IndiceMinimo = request.IndiceMinimo;
        vacante.FechaCierre = request.FechaCierre;

        var carrerasNuevas = request.CarrerasIds.ToHashSet();
        var carrerasActuales = vacante.Carreras.Select(c => c.CarreraId).ToHashSet();
        foreach (var carrera in vacante.Carreras.Where(c => !carrerasNuevas.Contains(c.CarreraId)).ToArray())
            db.VacanteCarreras.Remove(carrera);
        foreach (var carreraId in carrerasNuevas.Except(carrerasActuales))
            vacante.Carreras.Add(new VacanteCarrera { VacanteId = id, CarreraId = carreraId });

        var habilidadesNuevas = request.Habilidades.ToDictionary(h => h.HabilidadId);
        var habilidadesActuales = vacante.Habilidades.Select(h => h.HabilidadId).ToHashSet();
        foreach (var habilidad in vacante.Habilidades.ToArray())
        {
            if (habilidadesNuevas.TryGetValue(habilidad.HabilidadId, out var nueva)) habilidad.EsRequerida = nueva.EsRequerida;
            else db.VacanteHabilidades.Remove(habilidad);
        }
        foreach (var nueva in request.Habilidades.Where(h => !habilidadesActuales.Contains(h.HabilidadId)))
            vacante.Habilidades.Add(new VacanteHabilidad { VacanteId = id, HabilidadId = nueva.HabilidadId, EsRequerida = nueva.EsRequerida });

        await db.SaveChangesAsync(ct);
        return await ConsultasVacante.ObtenerAsync(db, id, fecha.Hoy(), ct);
    }
}

public sealed class CambiarEstadoVacanteHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task EjecutarAsync(Guid id, CambiarEstadoVacanteRequest request, CancellationToken ct)
    {
        await ReglasVacante.ExigirUsuarioEmpresaAsync(db, actual, ct);
        var vacante = await db.Vacantes.Include(v => v.Carreras).SingleOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new NoEncontradoException("La vacante no existe.");
        var hoy = fecha.Hoy();
        TransicionesVacante.Validar(ReglasVacante.EstadoEfectivo(vacante.Estado, vacante.FechaCierre, hoy), request.NuevoEstado);
        if (request.NuevoEstado == EstadoVacante.Publicada)
        {
            if (vacante.FechaCierre < hoy)
                throw new ReglaNegocioException(CodigosError.VacanteNoDisponible,
                    "La vacante está vencida y no se puede publicar.");
            await ReglasVacante.ValidarSeleccionAsync(db, vacante.Carreras.Select(c => c.CarreraId).ToArray(), [], true, ct);
            vacante.FechaPublicacion ??= fecha.UtcAhora();
        }
        vacante.Estado = request.NuevoEstado;
        await db.SaveChangesAsync(ct);
    }
}
