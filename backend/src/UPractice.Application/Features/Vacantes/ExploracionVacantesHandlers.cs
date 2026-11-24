using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;

namespace UPractice.Application.Features.Vacantes;

public sealed class ExplorarVacantesHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<PaginaResultado<VacanteResumenDto>> EjecutarAsync(FiltroVacantesQuery filtro, CancellationToken ct)
    {
        var estudiante = await ReglasExploracionVacantes.ExigirEstudianteAsync(db, actual, ct);
        var consulta = ReglasExploracionVacantes.Filtrar(
            ReglasExploracionVacantes.Visibles(db, estudiante, fecha.Hoy()), filtro);
        var total = await consulta.CountAsync(ct);
        var desplazamiento = ((long)filtro.Pagina - 1) * filtro.TamanoPagina;
        IReadOnlyList<VacanteResumenDto> items = desplazamiento >= total ? [] :
            await ConsultasExploracionVacantes.ListarAsync(db,
                consulta.OrderByDescending(v => v.FechaPublicacion).ThenByDescending(v => v.Id)
                    .Skip((int)desplazamiento).Take(filtro.TamanoPagina), estudiante, ct);
        return new PaginaResultado<VacanteResumenDto>(items, filtro.Pagina, filtro.TamanoPagina,
            total, (int)Math.Ceiling(total / (double)filtro.TamanoPagina));
    }
}

public sealed class ObtenerVacanteEstudianteHandler(IAppDbContext db, ICurrentUser actual, IFechaHonduras fecha)
{
    public async Task<VacanteDetalleDto> EjecutarAsync(Guid id, CancellationToken ct)
    {
        var estudiante = await ReglasExploracionVacantes.ExigirEstudianteAsync(db, actual, ct);
        return await ConsultasExploracionVacantes.DetalleAsync(db,
            ReglasExploracionVacantes.Visibles(db, estudiante, fecha.Hoy()), id, estudiante, ct);
    }
}
