using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPractice.Domain.Entidades;

namespace UPractice.Infrastructure.Persistencia.Configuraciones;

internal sealed class UniversidadConfiguracion : IEntityTypeConfiguration<Universidad>
{
    public void Configure(EntityTypeBuilder<Universidad> b)
    {
        b.ToTable("Universidades");
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.Property(x => x.Siglas).HasMaxLength(15).IsRequired();
        b.Property(x => x.DominioCorreo).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Siglas).IsUnique();
        b.HasIndex(x => x.DominioCorreo).IsUnique();
    }
}

internal sealed class FacultadConfiguracion : IEntityTypeConfiguration<Facultad>
{
    public void Configure(EntityTypeBuilder<Facultad> b)
    {
        b.ToTable("Facultades");
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.HasOne(x => x.Universidad).WithMany(u => u.Facultades).HasForeignKey(x => x.UniversidadId);
        b.HasIndex(x => new { x.UniversidadId, x.Nombre }).IsUnique();
    }
}

internal sealed class CarreraConfiguracion : IEntityTypeConfiguration<Carrera>
{
    public void Configure(EntityTypeBuilder<Carrera> b)
    {
        b.ToTable("Carreras");
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.HasOne(x => x.Facultad).WithMany(f => f.Carreras).HasForeignKey(x => x.FacultadId);
        b.HasIndex(x => new { x.FacultadId, x.Nombre }).IsUnique();
    }
}

internal sealed class HabilidadConfiguracion : IEntityTypeConfiguration<Habilidad>
{
    public void Configure(EntityTypeBuilder<Habilidad> b)
    {
        b.ToTable("Habilidades");
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        // La unicidad sin distinguir mayúsculas es un índice sobre lower("Nombre") creado en la migración inicial.
    }
}
