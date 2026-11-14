namespace UPractice.Domain.Entidades;

public class EstudianteHabilidad
{
    public Guid EstudianteId { get; set; }
    public Guid HabilidadId { get; set; }

    public Usuario Estudiante { get; set; } = null!;
    public Habilidad Habilidad { get; set; } = null!;
}
