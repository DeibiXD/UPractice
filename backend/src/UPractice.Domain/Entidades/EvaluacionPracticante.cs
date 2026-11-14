namespace UPractice.Domain.Entidades;

/// <summary>La empresa evalúa al practicante al cierre (Sprint 2).</summary>
public class EvaluacionPracticante : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmpresaId { get; set; }
    public Guid PracticaId { get; set; }
    public short Calificacion { get; set; }
    public string? Comentario { get; set; }

    public Practica Practica { get; set; } = null!;
}
