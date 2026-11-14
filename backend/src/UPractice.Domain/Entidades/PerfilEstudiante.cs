using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

/// <summary>Datos académicos del estudiante, verificados por la coordinación (D-04).</summary>
public class PerfilEstudiante
{
    public Guid UsuarioId { get; set; }
    public string NumeroCuenta { get; set; } = null!;
    public Guid CarreraId { get; set; }
    public decimal IndiceAcademico { get; set; }
    /// <summary>Consentimiento para que la empresa vea el índice (HU-02, HU-E05).</summary>
    public bool MostrarIndice { get; set; }
    public EstadoVerificacion EstadoVerificacion { get; set; } = EstadoVerificacion.Pendiente;
    public string? ObservacionVerificacion { get; set; }
    public Guid? VerificadoPorId { get; set; }
    public DateTime? FechaVerificacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Carrera Carrera { get; set; } = null!;
    public Usuario? VerificadoPor { get; set; }
}
