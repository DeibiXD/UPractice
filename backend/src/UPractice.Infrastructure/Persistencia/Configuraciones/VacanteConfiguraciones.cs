using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia.Configuraciones;

internal sealed class VacanteConfiguracion : IEntityTypeConfiguration<Vacante>
{
    public void Configure(EntityTypeBuilder<Vacante> b)
    {
        b.ToTable("Vacantes", t =>
        {
            t.HasCheckConstraint("CK_Vacantes_Cupos", "\"Cupos\" BETWEEN 1 AND 50");
            t.HasCheckConstraint("CK_Vacantes_IndiceMinimo", "\"IndiceMinimo\" IS NULL OR \"IndiceMinimo\" BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_Vacantes_Modalidad", Check.Enum<Modalidad>("Modalidad"));
            t.HasCheckConstraint("CK_Vacantes_Estado", Check.Enum<EstadoVacante>("Estado"));
        });
        // Clave alterna (Id, EmpresaId): la referencian las FK compuestas de las hijas (D-22).
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId });
        b.Property(x => x.Titulo).HasMaxLength(120).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Modalidad).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Ubicacion).HasMaxLength(120).IsRequired();
        b.Property(x => x.IndiceMinimo).HasPrecision(5, 2);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.CreadaPor).WithMany().HasForeignKey(x => x.CreadaPorId);
        b.HasIndex(x => new { x.EmpresaId, x.Estado });
        b.HasIndex(x => new { x.Estado, x.FechaCierre });
    }
}

internal sealed class VacanteCarreraConfiguracion : IEntityTypeConfiguration<VacanteCarrera>
{
    public void Configure(EntityTypeBuilder<VacanteCarrera> b)
    {
        b.ToTable("VacanteCarreras");
        b.HasKey(x => new { x.VacanteId, x.CarreraId });
        b.HasOne(x => x.Vacante).WithMany(v => v.Carreras).HasForeignKey(x => x.VacanteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Carrera).WithMany().HasForeignKey(x => x.CarreraId);
    }
}

internal sealed class VacanteHabilidadConfiguracion : IEntityTypeConfiguration<VacanteHabilidad>
{
    public void Configure(EntityTypeBuilder<VacanteHabilidad> b)
    {
        b.ToTable("VacanteHabilidades");
        b.HasKey(x => new { x.VacanteId, x.HabilidadId });
        b.HasOne(x => x.Vacante).WithMany(v => v.Habilidades).HasForeignKey(x => x.VacanteId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Habilidad).WithMany().HasForeignKey(x => x.HabilidadId);
    }
}
