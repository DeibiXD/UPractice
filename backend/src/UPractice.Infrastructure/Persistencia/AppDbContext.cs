using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Domain.Entidades;

namespace UPractice.Infrastructure.Persistencia;

/// <summary>
/// Contexto de EF Core. T-03 agrega aquí los filtros globales por tenant (HasQueryFilter, §3.3).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Universidad> Universidades => Set<Universidad>();
    public DbSet<Facultad> Facultades => Set<Facultad>();
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<HabilitacionEmpresa> HabilitacionesEmpresa => Set<HabilitacionEmpresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PerfilEstudiante> PerfilesEstudiante => Set<PerfilEstudiante>();
    public DbSet<Habilidad> Habilidades => Set<Habilidad>();
    public DbSet<EstudianteHabilidad> EstudianteHabilidades => Set<EstudianteHabilidad>();
    public DbSet<Curriculum> Curriculums => Set<Curriculum>();
    public DbSet<Vacante> Vacantes => Set<Vacante>();
    public DbSet<VacanteCarrera> VacanteCarreras => Set<VacanteCarrera>();
    public DbSet<VacanteHabilidad> VacanteHabilidades => Set<VacanteHabilidad>();
    public DbSet<Postulacion> Postulaciones => Set<Postulacion>();
    public DbSet<HistorialPostulacion> HistorialPostulacion => Set<HistorialPostulacion>();
    public DbSet<Practica> Practicas => Set<Practica>();
    public DbSet<DocumentoPractica> DocumentosPractica => Set<DocumentoPractica>();
    public DbSet<EvaluacionEmpresa> EvaluacionesEmpresa => Set<EvaluacionEmpresa>();
    public DbSet<EvaluacionPracticante> EvaluacionesPracticante => Set<EvaluacionPracticante>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Nada se borra en cascada salvo lo configurado explícitamente (tablas puente de la vacante):
        // borrar una empresa, usuario o vacante con historial debe fallar, no arrastrar datos.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            if (fk.DeleteBehavior == DeleteBehavior.Cascade && !EsTablaPuenteDeVacante(fk.DeclaringEntityType.ClrType))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static bool EsTablaPuenteDeVacante(Type tipo) => tipo == typeof(VacanteCarrera) || tipo == typeof(VacanteHabilidad);
}
