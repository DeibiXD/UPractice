using Microsoft.EntityFrameworkCore;
using UPractice.Domain.Entidades;

namespace UPractice.Application.Abstracciones;

/// <summary>
/// Puerta de Application a la base de datos (D-21). Los handlers consultan con LINQ sobre estos DbSet;
/// la implementación (AppDbContext, en Infrastructure) aplica los filtros globales por tenant (T-03).
/// </summary>
public interface IAppDbContext
{
    DbSet<Universidad> Universidades { get; }
    DbSet<Facultad> Facultades { get; }
    DbSet<Carrera> Carreras { get; }
    DbSet<Empresa> Empresas { get; }
    DbSet<HabilitacionEmpresa> HabilitacionesEmpresa { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<PerfilEstudiante> PerfilesEstudiante { get; }
    DbSet<Habilidad> Habilidades { get; }
    DbSet<EstudianteHabilidad> EstudianteHabilidades { get; }
    DbSet<Curriculum> Curriculums { get; }
    DbSet<Vacante> Vacantes { get; }
    DbSet<VacanteCarrera> VacanteCarreras { get; }
    DbSet<VacanteHabilidad> VacanteHabilidades { get; }
    DbSet<Postulacion> Postulaciones { get; }
    DbSet<HistorialPostulacion> HistorialPostulacion { get; }
    DbSet<Practica> Practicas { get; }
    DbSet<DocumentoPractica> DocumentosPractica { get; }
    DbSet<EvaluacionEmpresa> EvaluacionesEmpresa { get; }
    DbSet<EvaluacionPracticante> EvaluacionesPracticante { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
