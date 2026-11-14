namespace UPractice.Domain.Entidades;

/// <summary>Tabla puente: hereda el aislamiento de su vacante (D-22).</summary>
public class VacanteCarrera
{
    public Guid VacanteId { get; set; }
    public Guid CarreraId { get; set; }

    public Vacante Vacante { get; set; } = null!;
    public Carrera Carrera { get; set; } = null!;
}
