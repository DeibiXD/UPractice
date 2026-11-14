namespace UPractice.Domain.Entidades;

/// <summary>Universidad cliente de la plataforma. Su dominio de correo identifica a sus estudiantes (D-03).</summary>
public class Universidad
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = null!;
    public string Siglas { get; set; } = null!;
    public string DominioCorreo { get; set; } = null!;
    public bool Activa { get; set; } = true;

    public ICollection<Facultad> Facultades { get; set; } = [];
}
