using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    /// <summary>Abre una transacción para operaciones que deben ser atómicas (se revierte si no se confirma).</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Bloquea la fila de la vacante hasta que termine la transacción actual (SELECT ... FOR UPDATE). Sirve para serializar
    /// operaciones que dependen de un conteo, como los cupos de oferta. Debe llamarse dentro de BeginTransactionAsync.
    /// El id debe venir de una consulta ya filtrada por tenant: el SQL directo no aplica los filtros globales.
    /// </summary>
    Task BloquearVacanteAsync(Guid vacanteId, CancellationToken ct = default);
}
