namespace UPractice.Domain.Entidades;

/// <summary>El estudiante evalúa a la empresa (Sprint 2). El autor se obtiene por la práctica y no se muestra a la empresa (D-11).</summary>
public class EvaluacionEmpresa : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmpresaId { get; set; }
    public Guid PracticaId { get; set; }
    public short Calificacion { get; set; }
    public string? Comentario { get; set; }

    public Practica Practica { get; set; } = null!;
}
