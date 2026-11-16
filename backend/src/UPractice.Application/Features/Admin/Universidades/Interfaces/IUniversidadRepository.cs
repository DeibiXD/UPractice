using UPractice.Application.Features.Admin.Universidades.DTOs;
using UPractice.Domain.Entidades;

namespace UPractice.Application.Features.Admin.Universidades.Interfaces;

/// <summary>Acceso a persistencia de universidades para administración.</summary>
public interface IUniversidadRepository
{
    Task<IReadOnlyList<UniversidadAdminResponseDto>> GetAllAdminAsync(CancellationToken ct);
    Task<IReadOnlyList<Universidad>> GetActivasAsync(CancellationToken ct);
    Task<Universidad?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<UniversidadAdminResponseDto?> GetAdminByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExisteSiglasAsync(string siglasNormalizadas, Guid? exceptoId, CancellationToken ct);
    Task<bool> ExisteDominioAsync(string dominioNormalizado, Guid? exceptoId, CancellationToken ct);
    Task AddAsync(Universidad universidad, CancellationToken ct);
    Task UpdateAsync(Universidad universidad, CancellationToken ct);
    Task<bool> TieneUsuariosAsociadosAsync(Guid universidadId, CancellationToken ct);
}
