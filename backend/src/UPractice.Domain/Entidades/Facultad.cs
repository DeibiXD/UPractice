namespace UPractice.Domain.Entidades;

public class Facultad
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniversidadId { get; set; }
    public string Nombre { get; set; } = null!;

    public Universidad Universidad { get; set; } = null!;
    public ICollection<Carrera> Carreras { get; set; } = [];
}
