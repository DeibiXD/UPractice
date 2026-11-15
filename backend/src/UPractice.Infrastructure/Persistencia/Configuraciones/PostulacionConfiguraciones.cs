using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia.Configuraciones;

internal sealed class PostulacionConfiguracion : IEntityTypeConfiguration<Postulacion>
{
    public void Configure(EntityTypeBuilder<Postulacion> b)
    {
        b.ToTable("Postulaciones", t => t.HasCheckConstraint("CK_Postulaciones_Estado", Check.Enum<EstadoPostulacion>("Estado")));
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId });
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId);
        // FK compuesta: el EmpresaId de la postulación siempre es el de su vacante (§3.7).
        b.HasOne(x => x.Vacante).WithMany(v => v.Postulaciones)
            .HasForeignKey(x => new { x.VacanteId, x.EmpresaId })
            .HasPrincipalKey(v => new { v.Id, v.EmpresaId });
        b.HasOne(x => x.Estudiante).WithMany().HasForeignKey(x => x.EstudianteId);
        b.HasOne(x => x.Curriculum).WithMany().HasForeignKey(x => x.CurriculumId);

        b.HasIndex(x => new { x.VacanteId, x.EstudianteId }).IsUnique(); // D-20
        b.HasIndex(x => new { x.EmpresaId, x.Estado });
        b.HasIndex(x => x.EstudianteId);
    }
}

internal sealed class HistorialPostulacionConfiguracion : IEntityTypeConfiguration<HistorialPostulacion>
{
    public void Configure(EntityTypeBuilder<HistorialPostulacion> b)
    {
        b.ToTable("HistorialPostulacion", t =>
        {
            t.HasCheckConstraint("CK_HistorialPostulacion_EstadoNuevo", Check.Enum<EstadoPostulacion>("EstadoNuevo"));
            t.HasCheckConstraint("CK_HistorialPostulacion_EstadoAnterior",
                "\"EstadoAnterior\" IS NULL OR " + Check.Enum<EstadoPostulacion>("EstadoAnterior"));
        });
        b.Property(x => x.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.EstadoNuevo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Comentario).HasMaxLength(500);

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.Postulacion).WithMany(p => p.Historial)
            .HasForeignKey(x => new { x.PostulacionId, x.EmpresaId })
            .HasPrincipalKey(p => new { p.Id, p.EmpresaId });
        b.HasOne(x => x.CambiadoPor).WithMany().HasForeignKey(x => x.CambiadoPorId);
        b.HasIndex(x => new { x.PostulacionId, x.Fecha });
    }
}
