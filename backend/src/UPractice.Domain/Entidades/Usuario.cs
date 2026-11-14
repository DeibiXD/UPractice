using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

public class Usuario
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Siempre en minúsculas (UK).</summary>
    public string Correo { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Nombre1 { get; set; } = null!;
    public string? Nombre2 { get; set; }
    public string Apellido1 { get; set; } = null!;
    public string? Apellido2 { get; set; }
    public Rol Rol { get; set; }
    /// <summary>Solo AdminEmpresa y Reclutador (D-19).</summary>
    public Guid? EmpresaId { get; set; }
    /// <summary>Solo Estudiante y Coordinador.</summary>
    public Guid? UniversidadId { get; set; }
    public bool Activo { get; set; } = true;
    /// <summary>Cuenta con contraseña temporal (D-17).</summary>
    public bool DebeCambiarPassword { get; set; }

    public Empresa? Empresa { get; set; }
    public Universidad? Universidad { get; set; }
    public PerfilEstudiante? Perfil { get; set; }
    public ICollection<EstudianteHabilidad> Habilidades { get; set; } = [];
    public ICollection<Curriculum> Curriculums { get; set; } = [];

    /// <summary>"Nombre1 Nombre2 Apellido1 Apellido2" sin espacios dobles. No se guarda (D-23).</summary>
    public string NombreCompleto => ArmarNombreCompleto(Nombre1, Nombre2, Apellido1, Apellido2);

    public static string ArmarNombreCompleto(string nombre1, string? nombre2, string apellido1, string? apellido2)
        => string.Join(' ', new[] { nombre1, nombre2, apellido1, apellido2 }.Where(p => !string.IsNullOrWhiteSpace(p)));
}
