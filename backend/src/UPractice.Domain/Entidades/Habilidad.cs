namespace UPractice.Domain.Entidades;

public class Habilidad
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = null!;
}
