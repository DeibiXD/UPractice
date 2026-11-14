using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

public class Postulacion : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Siempre igual al de la vacante: FK compuesta (VacanteId, EmpresaId).</summary>
    public Guid EmpresaId { get; set; }
    public Guid VacanteId { get; set; }
    public Guid EstudianteId { get; set; }
    public Guid CurriculumId { get; set; }
    public EstadoPostulacion Estado { get; set; } = EstadoPostulacion.Enviada;
    /// <summary>Foto del cumplimiento del índice al postular (D-06).</summary>
    public bool CumpleRequisitos { get; set; }
    public DateTime FechaPostulacion { get; set; } = DateTime.UtcNow;

    public Empresa Empresa { get; set; } = null!;
    public Vacante Vacante { get; set; } = null!;
    public Usuario Estudiante { get; set; } = null!;
    public Curriculum Curriculum { get; set; } = null!;
    public ICollection<HistorialPostulacion> Historial { get; set; } = [];
    public Practica? Practica { get; set; }
}
