namespace UPractice.Domain.Entidades;

/// <summary>Versión del CV. El PDF vive en el almacenamiento; aquí solo la llave (§3.8).</summary>
public class Curriculum
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EstudianteId { get; set; }
    public string NombreVersion { get; set; } = null!;
    public string ArchivoKey { get; set; } = null!;
    public bool EsPrincipal { get; set; }
    /// <summary>Baja lógica: el archivo se conserva porque puede estar adjunto a postulaciones (HU-03).</summary>
    public bool Eliminado { get; set; }

    public Usuario Estudiante { get; set; } = null!;
}
