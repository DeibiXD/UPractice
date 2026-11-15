using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

public class Vacante : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmpresaId { get; set; }
    public Guid CreadaPorId { get; set; }
    public string Titulo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public Modalidad Modalidad { get; set; }
    public string Ubicacion { get; set; } = null!;
    public int Cupos { get; set; }
    public bool Remunerada { get; set; }
    /// <summary>Requisito deseable, no excluyente (D-06).</summary>
    public decimal? IndiceMinimo { get; set; }
    public EstadoVacante Estado { get; set; } = EstadoVacante.Borrador;
    public DateOnly FechaCierre { get; set; }
    public DateTime? FechaPublicacion { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public Usuario CreadaPor { get; set; } = null!;
    public ICollection<VacanteCarrera> Carreras { get; set; } = [];
    public ICollection<VacanteHabilidad> Habilidades { get; set; } = [];
    public ICollection<Postulacion> Postulaciones { get; set; } = [];
}
