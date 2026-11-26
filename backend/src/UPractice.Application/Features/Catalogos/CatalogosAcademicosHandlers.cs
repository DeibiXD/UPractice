using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Comun.Contratos;

namespace UPractice.Application.Features.Catalogos;

/// <summary>
/// GET /catalogos/carreras?universidadId= (HU-02, HU-E03). Solo carreras activas de universidades activas.
/// Sin <c>universidadId</c> devuelve las de todas las universidades activas.
/// </summary>
public sealed class ListarCarrerasHandler(IAppDbContext db)
{
    public async Task<IReadOnlyList<CarreraDto>> EjecutarAsync(Guid? universidadId, CancellationToken ct) =>
        await db.Carreras.AsNoTracking()
            .Where(c => c.Activa && c.Facultad.Universidad.Activa
                        && (universidadId == null || c.Facultad.UniversidadId == universidadId))
            .OrderBy(c => c.Facultad.Nombre).ThenBy(c => c.Nombre)
            .Select(c => new CarreraDto(c.Id, c.Nombre, c.Facultad.Nombre, c.Facultad.UniversidadId))
            .ToListAsync(ct);
}

/// <summary>GET /catalogos/habilidades (HU-02, HU-E03).</summary>
public sealed class ListarHabilidadesHandler(IAppDbContext db)
{
    public async Task<IReadOnlyList<HabilidadDto>> EjecutarAsync(CancellationToken ct) =>
        await db.Habilidades.AsNoTracking()
            .OrderBy(h => h.Nombre)
            .Select(h => new HabilidadDto(h.Id, h.Nombre))
            .ToListAsync(ct);
}
