using UPractice.Domain.Enums;

namespace UPractice.Domain.Entidades;

/// <summary>Permiso de una universidad para que la empresa reclute a sus estudiantes (D-05).</summary>
public class HabilitacionEmpresa : IEntidadTenant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmpresaId { get; set; }
    public Guid UniversidadId { get; set; }
    public EstadoHabilitacion Estado { get; set; } = EstadoHabilitacion.Pendiente;
    public string? MotivoRechazo { get; set; }
    public Guid? ResueltaPorId { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public DateTime? FechaResolucion { get; set; }

    public Empresa Empresa { get; set; } = null!;
    public Universidad Universidad { get; set; } = null!;
    public Usuario? ResueltaPor { get; set; }
}
