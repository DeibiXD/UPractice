using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia.Configuraciones;

internal sealed class PracticaConfiguracion : IEntityTypeConfiguration<Practica>
{
    public void Configure(EntityTypeBuilder<Practica> b)
    {
        b.ToTable("Practicas", t =>
        {
            t.HasCheckConstraint("CK_Practicas_Estado", Check.Enum<EstadoPractica>("Estado"));
            t.HasCheckConstraint("CK_Practicas_Horas", "\"HorasCompletadas\" IS NULL OR \"HorasCompletadas\" >= 0");
            t.HasCheckConstraint("CK_Practicas_Fechas", "\"FechaFin\" IS NULL OR \"FechaFin\" >= \"FechaInicio\"");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId });
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.Postulacion).WithOne(p => p.Practica)
            .HasForeignKey<Practica>(x => new { x.PostulacionId, x.EmpresaId })
            .HasPrincipalKey<Postulacion>(p => new { p.Id, p.EmpresaId });
        b.HasIndex(x => x.PostulacionId).IsUnique();
    }
}

internal sealed class DocumentoPracticaConfiguracion : IEntityTypeConfiguration<DocumentoPractica>
{
    public void Configure(EntityTypeBuilder<DocumentoPractica> b)
    {
        b.ToTable("DocumentosPractica", t =>
        {
            t.HasCheckConstraint("CK_DocumentosPractica_Tipo", Check.Enum<TipoDocumentoPractica>("TipoDocumento"));
            t.HasCheckConstraint("CK_DocumentosPractica_Estado", Check.Enum<EstadoRevisionDocumento>("EstadoRevision"));
        });
        b.Property(x => x.TipoDocumento).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.EstadoRevision).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ArchivoKey).HasMaxLength(300).IsRequired();
        b.Property(x => x.ObservacionRevision).HasMaxLength(500);
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.Practica).WithMany(p => p.Documentos)
            .HasForeignKey(x => new { x.PracticaId, x.EmpresaId })
            .HasPrincipalKey(p => new { p.Id, p.EmpresaId });
        b.HasOne(x => x.SubidoPor).WithMany().HasForeignKey(x => x.SubidoPorId);
        b.HasOne(x => x.RevisadoPor).WithMany().HasForeignKey(x => x.RevisadoPorId);
    }
}

internal sealed class EvaluacionEmpresaConfiguracion : IEntityTypeConfiguration<EvaluacionEmpresa>
{
    public void Configure(EntityTypeBuilder<EvaluacionEmpresa> b)
    {
        b.ToTable("EvaluacionesEmpresa", t => t.HasCheckConstraint("CK_EvaluacionesEmpresa_Calificacion", "\"Calificacion\" BETWEEN 1 AND 5"));
        b.Property(x => x.Comentario).HasMaxLength(1000);
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.Practica).WithOne(p => p.EvaluacionEmpresa)
            .HasForeignKey<EvaluacionEmpresa>(x => new { x.PracticaId, x.EmpresaId })
            .HasPrincipalKey<Practica>(p => new { p.Id, p.EmpresaId });
        b.HasIndex(x => x.PracticaId).IsUnique();
    }
}

internal sealed class EvaluacionPracticanteConfiguracion : IEntityTypeConfiguration<EvaluacionPracticante>
{
    public void Configure(EntityTypeBuilder<EvaluacionPracticante> b)
    {
        b.ToTable("EvaluacionesPracticante", t => t.HasCheckConstraint("CK_EvaluacionesPracticante_Calificacion", "\"Calificacion\" BETWEEN 1 AND 5"));
        b.Property(x => x.Comentario).HasMaxLength(1000);
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.Practica).WithOne(p => p.EvaluacionPracticante)
            .HasForeignKey<EvaluacionPracticante>(x => new { x.PracticaId, x.EmpresaId })
            .HasPrincipalKey<Practica>(p => new { p.Id, p.EmpresaId });
        b.HasIndex(x => x.PracticaId).IsUnique();
    }
}
