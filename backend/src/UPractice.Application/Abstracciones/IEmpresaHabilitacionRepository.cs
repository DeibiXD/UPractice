using UPractice.Domain.Entidades;

namespace UPractice.Application.Abstracciones;

/// <summary>Persistencia de empresas y habilitaciones; la implementación reside en Infrastructure.</summary>
public interface IEmpresaHabilitacionRepository
{
    Task<bool> ExisteRtnAsync(string rtn, CancellationToken ct);
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct);
    Task<bool> SonUniversidadesActivasAsync(IReadOnlyList<Guid> universidadesIds, CancellationToken ct);
    Task RegistrarTransaccionalmenteAsync(
        UPractice.Domain.Entidades.Empresa empresa,
        Usuario admin,
        IReadOnlyCollection<Guid> universidadesIds,
        CancellationToken ct);
    Task<IReadOnlyList<HabilitacionEmpresa>> ListarAsync(Guid empresaId, CancellationToken ct);
    Task<HabilitacionEmpresa?> ObtenerAsync(Guid empresaId, Guid universidadId, CancellationToken ct);
    Task<Universidad?> ObtenerUniversidadActivaAsync(Guid universidadId, CancellationToken ct);
    Task GuardarSolicitudAsync(HabilitacionEmpresa habilitacion, CancellationToken ct);
    Task<bool> TieneHabilitacionAprobadaAsync(Guid empresaId, CancellationToken ct);
}
