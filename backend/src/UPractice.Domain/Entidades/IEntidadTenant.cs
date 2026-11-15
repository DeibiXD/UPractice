namespace UPractice.Domain.Entidades;

/// <summary>
/// Tabla transaccional del tenant: lleva EmpresaId como FK a Empresas (D-01, D-22).
/// El filtro global de T-03 se aplica a toda entidad que implemente esta interfaz.
/// </summary>
public interface IEntidadTenant
{
    Guid EmpresaId { get; }
}
