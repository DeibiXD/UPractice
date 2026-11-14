using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

/// <summary>Documento del expediente de la práctica (Sprint 2).</summary>
public class DocumentoPractica : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmpresaId { get; set; }
    public Guid PracticaId { get; set; }
    public TipoDocumentoPractica TipoDocumento { get; set; }
    public string ArchivoKey { get; set; } = null!;
    public Guid SubidoPorId { get; set; }
    public EstadoRevisionDocumento EstadoRevision { get; set; } = EstadoRevisionDocumento.Pendiente;
    public Guid? RevisadoPorId { get; set; }
    public string? ObservacionRevision { get; set; }

    public Practica Practica { get; set; } = null!;
    public Usuario SubidoPor { get; set; } = null!;
    public Usuario? RevisadoPor { get; set; }
}
