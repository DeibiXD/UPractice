using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia.Configuraciones;

internal sealed class EmpresaConfiguracion : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("Empresas", t =>
        {
            t.HasCheckConstraint("CK_Empresas_RTN", "\"RTN\" ~ '^[0-9]{14}$'");
            t.HasCheckConstraint("CK_Empresas_Suspension", "\"Activa\" OR \"MotivoSuspension\" IS NOT NULL");
        });
        b.Property(x => x.NombreComercial).HasMaxLength(150).IsRequired();
        b.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
        b.Property(x => x.Rtn).HasColumnName("RTN").HasMaxLength(14).IsFixedLength().IsRequired();
        b.Property(x => x.Sector).HasMaxLength(80).IsRequired();
        b.Property(x => x.SitioWeb).HasMaxLength(200);
        b.Property(x => x.MotivoSuspension).HasMaxLength(500);
        b.HasIndex(x => x.Rtn).IsUnique();
    }
}

internal sealed class HabilitacionEmpresaConfiguracion : IEntityTypeConfiguration<HabilitacionEmpresa>
{
    public void Configure(EntityTypeBuilder<HabilitacionEmpresa> b)
    {
        b.ToTable("HabilitacionesEmpresa", t =>
        {
            t.HasCheckConstraint("CK_HabilitacionesEmpresa_Estado", Check.Enum<EstadoHabilitacion>("Estado"));
            t.HasCheckConstraint("CK_HabilitacionesEmpresa_Motivo", "\"Estado\" <> 'Rechazada' OR \"MotivoRechazo\" IS NOT NULL");
        });
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MotivoRechazo).HasMaxLength(500);
        b.HasOne(x => x.Empresa).WithMany(e => e.Habilitaciones).HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.Universidad).WithMany().HasForeignKey(x => x.UniversidadId);
        b.HasOne(x => x.ResueltaPor).WithMany().HasForeignKey(x => x.ResueltaPorId);
        b.HasIndex(x => new { x.EmpresaId, x.UniversidadId }).IsUnique();
        b.HasIndex(x => new { x.UniversidadId, x.Estado });
    }
}
