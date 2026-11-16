using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Infrastructure.Persistencia;

internal sealed class EmpresaHabilitacionRepository(AppDbContext db) : IEmpresaHabilitacionRepository
{
    public Task<bool> ExisteRtnAsync(string rtn, CancellationToken ct) =>
        db.Empresas.AnyAsync(e => e.Rtn == rtn, ct);

    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.Correo == correo, ct);

    public async Task<bool> SonUniversidadesActivasAsync(IReadOnlyList<Guid> universidadesIds, CancellationToken ct)
    {
        if (universidadesIds is null || universidadesIds.Count == 0) return false;
        return await db.Universidades.CountAsync(
            u => u.Activa && universidadesIds.Contains(u.Id), ct) == universidadesIds.Count;
    }

    public async Task RegistrarTransaccionalmenteAsync(
        Empresa empresa,
        Usuario admin,
        IReadOnlyCollection<Guid> universidadesIds,
        CancellationToken ct)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.Empresas.Add(empresa);
            db.Usuarios.Add(admin);
            db.HabilitacionesEmpresa.AddRange(universidadesIds.Select(universidadId => new HabilitacionEmpresa
            {
                EmpresaId = empresa.Id,
                UniversidadId = universidadId,
                Estado = EstadoHabilitacion.Pendiente,
            }));

            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();

            if (await ExisteRtnAsync(empresa.Rtn, CancellationToken.None))
                throw new ConflictoException(CodigosError.RtnEnUso, "El RTN ya está registrado.");
            if (await ExisteCorreoAsync(admin.Correo, CancellationToken.None))
                throw new ConflictoException(CodigosError.CorreoEnUso, "El correo ya está registrado.");
            throw;
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<HabilitacionEmpresa>> ListarAsync(Guid empresaId, CancellationToken ct) =>
        await db.HabilitacionesEmpresa
            .AsNoTracking()
            .Where(h => h.EmpresaId == empresaId)
            .Include(h => h.Universidad)
            .OrderBy(h => h.Universidad.Siglas)
            .ToListAsync(ct);

    public Task<HabilitacionEmpresa?> ObtenerAsync(Guid empresaId, Guid universidadId, CancellationToken ct) =>
        db.HabilitacionesEmpresa.Include(h => h.Universidad).FirstOrDefaultAsync(
            h => h.EmpresaId == empresaId && h.UniversidadId == universidadId, ct);

    public Task<Universidad?> ObtenerUniversidadActivaAsync(Guid universidadId, CancellationToken ct) =>
        db.Universidades.FirstOrDefaultAsync(u => u.Id == universidadId && u.Activa, ct);

    public async Task GuardarSolicitudAsync(HabilitacionEmpresa habilitacion, CancellationToken ct)
    {
        if (db.Entry(habilitacion).State == EntityState.Detached)
            db.HabilitacionesEmpresa.Add(habilitacion);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await db.HabilitacionesEmpresa.AnyAsync(h =>
                    h.EmpresaId == habilitacion.EmpresaId
                    && h.UniversidadId == habilitacion.UniversidadId
                    && (h.Estado == EstadoHabilitacion.Pendiente || h.Estado == EstadoHabilitacion.Aprobada), ct))
                throw new ConflictoException(CodigosError.HabilitacionEnCurso,
                    "Ya existe una solicitud pendiente o aprobada para esta universidad.");
            throw;
        }
    }

    public Task<bool> TieneHabilitacionAprobadaAsync(Guid empresaId, CancellationToken ct) =>
        db.HabilitacionesEmpresa.AnyAsync(
            h => h.EmpresaId == empresaId && h.Estado == EstadoHabilitacion.Aprobada, ct);
}
