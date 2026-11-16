using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;

namespace UPractice.Application.Features.Catalogos;

/// <summary>Lista las universidades activas del catálogo público.</summary>
public sealed class ListarUniversidadesHandler(IAppDbContext db)
{
    public async Task<IReadOnlyList<UniversidadDto>> EjecutarAsync(CancellationToken ct) =>
        await db.Universidades
            .AsNoTracking()
            .Where(u => u.Activa)
            .OrderBy(u => u.Siglas)
            .Select(u => new UniversidadDto(u.Id, u.Nombre, u.Siglas, u.DominioCorreo))
            .ToListAsync(ct);
}
