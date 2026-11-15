using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia.Configuraciones;

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios", t =>
        {
            t.HasCheckConstraint("CK_Usuarios_Rol", Check.Enum<Rol>("Rol"));
            t.HasCheckConstraint("CK_Usuarios_CorreoMinusculas", "\"Correo\" = lower(\"Correo\")");
            // D-19: un usuario de empresa pertenece a una sola empresa; estudiantes y coordinadores, a una universidad.
            t.HasCheckConstraint("CK_Usuarios_Pertenencia",
                "(\"Rol\" IN ('AdminEmpresa', 'Reclutador') AND \"EmpresaId\" IS NOT NULL AND \"UniversidadId\" IS NULL)"
                + " OR (\"Rol\" IN ('Estudiante', 'Coordinador') AND \"UniversidadId\" IS NOT NULL AND \"EmpresaId\" IS NULL)"
                + " OR (\"Rol\" = 'SuperAdmin' AND \"EmpresaId\" IS NULL AND \"UniversidadId\" IS NULL)");
        });
        b.Property(x => x.Correo).HasMaxLength(150).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(x => x.Nombre1).HasMaxLength(50).IsRequired();
        b.Property(x => x.Nombre2).HasMaxLength(50);
        b.Property(x => x.Apellido1).HasMaxLength(50).IsRequired();
        b.Property(x => x.Apellido2).HasMaxLength(50);
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.NombreCompleto);
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId);
        b.HasOne(x => x.Universidad).WithMany().HasForeignKey(x => x.UniversidadId);
        b.HasIndex(x => x.Correo).IsUnique();
    }
}

internal sealed class PerfilEstudianteConfiguracion : IEntityTypeConfiguration<PerfilEstudiante>
{
    public void Configure(EntityTypeBuilder<PerfilEstudiante> b)
    {
        b.ToTable("PerfilesEstudiante", t =>
        {
            t.HasCheckConstraint("CK_PerfilesEstudiante_Indice", "\"IndiceAcademico\" BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_PerfilesEstudiante_Estado", Check.Enum<EstadoVerificacion>("EstadoVerificacion"));
            t.HasCheckConstraint("CK_PerfilesEstudiante_Observacion",
                "\"EstadoVerificacion\" <> 'Rechazado' OR \"ObservacionVerificacion\" IS NOT NULL");
        });
        b.HasKey(x => x.UsuarioId);
        b.Property(x => x.NumeroCuenta).HasMaxLength(20).IsRequired();
        b.Property(x => x.IndiceAcademico).HasPrecision(5, 2);
        b.Property(x => x.EstadoVerificacion).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ObservacionVerificacion).HasMaxLength(500);
        b.HasOne(x => x.Usuario).WithOne(u => u.Perfil).HasForeignKey<PerfilEstudiante>(x => x.UsuarioId);
        b.HasOne(x => x.Carrera).WithMany().HasForeignKey(x => x.CarreraId);
        b.HasOne(x => x.VerificadoPor).WithMany().HasForeignKey(x => x.VerificadoPorId);
        b.HasIndex(x => x.EstadoVerificacion);
    }
}

internal sealed class EstudianteHabilidadConfiguracion : IEntityTypeConfiguration<EstudianteHabilidad>
{
    public void Configure(EntityTypeBuilder<EstudianteHabilidad> b)
    {
        b.ToTable("EstudianteHabilidades");
        b.HasKey(x => new { x.EstudianteId, x.HabilidadId });
        b.HasOne(x => x.Estudiante).WithMany(u => u.Habilidades).HasForeignKey(x => x.EstudianteId);
        b.HasOne(x => x.Habilidad).WithMany().HasForeignKey(x => x.HabilidadId);
    }
}

internal sealed class CurriculumConfiguracion : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> b)
    {
        b.ToTable("Curriculums");
        b.Property(x => x.NombreVersion).HasMaxLength(60).IsRequired();
        b.Property(x => x.ArchivoKey).HasMaxLength(300).IsRequired();
        b.HasOne(x => x.Estudiante).WithMany(u => u.Curriculums).HasForeignKey(x => x.EstudianteId);
        // Un solo CV principal activo por estudiante.
        b.HasIndex(x => x.EstudianteId).IsUnique()
            .HasFilter("\"EsPrincipal\" AND NOT \"Eliminado\"")
            .HasDatabaseName("UX_Curriculums_PrincipalPorEstudiante");
        b.HasIndex(x => x.EstudianteId).HasDatabaseName("IX_Curriculums_EstudianteId");
    }
}
