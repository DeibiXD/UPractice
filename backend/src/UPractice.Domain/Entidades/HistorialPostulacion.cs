using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

/// <summary>Cada transición de la postulación (§3.6). La fecha del último cambio se calcula de aquí (D-23).</summary>
public class HistorialPostulacion : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmpresaId { get; set; }
    public Guid PostulacionId { get; set; }
    public EstadoPostulacion? EstadoAnterior { get; set; }
    public EstadoPostulacion EstadoNuevo { get; set; }
    public Guid CambiadoPorId { get; set; }
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public Postulacion Postulacion { get; set; } = null!;
    public Usuario CambiadoPor { get; set; } = null!;
}
