namespace UPractice.Domain.Entidades;

/// <summary>Tabla puente: hereda el aislamiento de su vacante (D-22).</summary>
public class VacanteHabilidad
{
    public Guid VacanteId { get; set; }
    public Guid HabilidadId { get; set; }
    public bool EsRequerida { get; set; }

    public Vacante Vacante { get; set; } = null!;
    public Habilidad Habilidad { get; set; } = null!;
}
