using Microsoft.EntityFrameworkCore;
using UPractice.Application.Features.Admin.Universidades.DTOs;
using UPractice.Application.Features.Admin.Universidades.Interfaces;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Infrastructure.Persistencia;

internal sealed class UniversidadRepository(AppDbContext db) : IUniversidadRepository
{
    public async Task<IReadOnlyList<UniversidadAdminResponseDto>> GetAllAdminAsync(CancellationToken ct) =>
        await db.Universidades
            .AsNoTracking()
            .OrderBy(u => u.Siglas)
            .Select(u => new UniversidadAdminResponseDto(
                u.Id,
                u.Nombre,
                u.Siglas,
                u.DominioCorreo,
                u.Activa,
                db.Usuarios.Count(usuario => usuario.UniversidadId == u.Id && usuario.Rol == Rol.Estudiante),
                db.Usuarios.Count(usuario => usuario.UniversidadId == u.Id && usuario.Rol == Rol.Coordinador)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Universidad>> GetActivasAsync(CancellationToken ct) =>
        await db.Universidades
            .AsNoTracking()
            .Where(u => u.Activa)
            .OrderBy(u => u.Siglas)
            .ToListAsync(ct);

    public Task<Universidad?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Universidades.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<UniversidadAdminResponseDto?> GetAdminByIdAsync(Guid id, CancellationToken ct) =>
        db.Universidades
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UniversidadAdminResponseDto(
                u.Id,
                u.Nombre,
                u.Siglas,
                u.DominioCorreo,
                u.Activa,
                db.Usuarios.Count(usuario => usuario.UniversidadId == u.Id && usuario.Rol == Rol.Estudiante),
                db.Usuarios.Count(usuario => usuario.UniversidadId == u.Id && usuario.Rol == Rol.Coordinador)))
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExisteSiglasAsync(string siglasNormalizadas, Guid? exceptoId, CancellationToken ct)
    {
        var consulta = db.Universidades.AsNoTracking()
            .Where(u => u.Siglas.ToUpper() == siglasNormalizadas);
        if (exceptoId is { } id)
            consulta = consulta.Where(u => u.Id != id);
        return consulta.AnyAsync(ct);
    }

    public Task<bool> ExisteDominioAsync(string dominioNormalizado, Guid? exceptoId, CancellationToken ct)
    {
        var consulta = db.Universidades.AsNoTracking()
            .Where(u => u.DominioCorreo.ToLower() == dominioNormalizado);
        if (exceptoId is { } id)
            consulta = consulta.Where(u => u.Id != id);
        return consulta.AnyAsync(ct);
    }

    public async Task AddAsync(Universidad universidad, CancellationToken ct)
    {
        db.Universidades.Add(universidad);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await ExisteSiglasAsync(universidad.Siglas, null, ct))
                throw new ConflictoException(CodigosError.SiglasEnUso, "Las siglas ya están registradas.");
            if (await ExisteDominioAsync(universidad.DominioCorreo, null, ct))
                throw new ConflictoException(CodigosError.DominioEnUso, "El dominio ya está registrado.");
            throw;
        }
    }

    public async Task UpdateAsync(Universidad universidad, CancellationToken ct)
    {
        if (db.Entry(universidad).State == EntityState.Detached)
            db.Universidades.Update(universidad);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await ExisteSiglasAsync(universidad.Siglas, universidad.Id, ct))
                throw new ConflictoException(CodigosError.SiglasEnUso, "Las siglas ya están registradas.");
            if (await ExisteDominioAsync(universidad.DominioCorreo, universidad.Id, ct))
                throw new ConflictoException(CodigosError.DominioEnUso, "El dominio ya está registrado.");
            throw;
        }
    }

    public Task<bool> TieneUsuariosAsociadosAsync(Guid universidadId, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.UniversidadId == universidadId, ct);
}
