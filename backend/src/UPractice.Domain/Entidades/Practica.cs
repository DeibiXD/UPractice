using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

/// <summary>Práctica en curso (Sprint 2). Nace de una postulación Aceptada.</summary>
public class Practica : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmpresaId { get; set; }
    public Guid PostulacionId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public int? HorasCompletadas { get; set; }
    public EstadoPractica Estado { get; set; } = EstadoPractica.EnCurso;
    /// <summary>Se registra al cierre (HU-E09).</summary>
    public bool? FueContratado { get; set; }

    public Postulacion Postulacion { get; set; } = null!;
    public ICollection<DocumentoPractica> Documentos { get; set; } = [];
    public EvaluacionEmpresa? EvaluacionEmpresa { get; set; }
    public EvaluacionPracticante? EvaluacionPracticante { get; set; }
}
