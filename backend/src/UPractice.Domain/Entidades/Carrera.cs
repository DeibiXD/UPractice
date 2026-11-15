namespace UPractice.Domain.Entidades;

public class Carrera
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FacultadId { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activa { get; set; } = true;

    public Facultad Facultad { get; set; } = null!;
}
