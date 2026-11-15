using UPractice.Domain.Enums;

namespace UPractice.Application.Abstracciones;

/// <summary>
/// Usuario de la petición actual, leído de los claims del token (S3.1). El EmpresaId y el UniversidadId
/// salen SIEMPRE de aquí, nunca del body ni de la URL. Sin token, todas las propiedades son null.
/// </summary>
public interface ICurrentUser
{
    Guid? UsuarioId { get; }
    Rol? Rol { get; }
    /// <summary>Claim "empresa_id": solo AdminEmpresa y Reclutador.</summary>
    Guid? EmpresaId { get; }
    /// <summary>Claim "universidad_id": solo Estudiante y Coordinador.</summary>
    Guid? UniversidadId { get; }
}
