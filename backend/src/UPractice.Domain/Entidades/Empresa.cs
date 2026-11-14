namespace UPractice.Domain.Entidades;

/// <summary>Tenant de la plataforma (D-01).</summary>
public class Empresa
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string NombreComercial { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public string Rtn { get; set; } = null!;
    public string Sector { get; set; } = null!;
    public string? SitioWeb { get; set; }
    /// <summary>false = suspendida por el SuperAdmin (HU-A04).</summary>
    public bool Activa { get; set; } = true;
    public string? MotivoSuspension { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public ICollection<HabilitacionEmpresa> Habilitaciones { get; set; } = [];
}
