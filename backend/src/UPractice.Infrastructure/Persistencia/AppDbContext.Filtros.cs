using Microsoft.EntityFrameworkCore;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;

namespace UPractice.Infrastructure.Persistencia;

/// <summary>
/// Filtros globales (HasQueryFilter) que aíslan los datos de cada tenant (lineamiento de la Fase 2, S3.2 y S3.3).
///
/// - Usuario de empresa: solo filas con EmpresaId = claim "empresa_id".
/// - Estudiante: solo lo suyo (UsuarioId = "sub") y vacantes publicadas de empresas aprobadas en su universidad.
/// - Coordinador: lo de los estudiantes de su universidad ("universidad_id").
/// - SuperAdmin: solo HabilitacionesEmpresa (D-14).
/// - Sin token: ninguna fila.
///
/// Los valores se leen de propiedades del propio DbContext: EF las convierte en parámetros en cada consulta,
/// así el modelo se compila una sola vez y cada petición usa los datos de su propio usuario.
/// Usuarios, Empresas y los catálogos no se filtran (el login busca por correo sin usuario autenticado);
/// sus consultas se limitan en Application. IgnoreQueryFilters() solo se permite en login, seed y jobs internos.
/// </summary>
public sealed partial class AppDbContext
{
    private Guid? EmpresaActualId => usuario.EmpresaId;
    private Guid? UniversidadActualId => usuario.UniversidadId;
    private Guid? UsuarioActualId => usuario.UsuarioId;
    private Rol? RolActual => usuario.Rol;

    private void AplicarFiltrosGlobales(ModelBuilder b)
    {
        b.Entity<HabilitacionEmpresa>().HasQueryFilter(h =>
               (EmpresaActualId != null && h.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Coordinador && h.UniversidadId == UniversidadActualId)
            // El estudiante necesita saber qué empresas están aprobadas en su universidad (filtro de Vacantes).
            || (RolActual == Rol.Estudiante && h.UniversidadId == UniversidadActualId && h.Estado == EstadoHabilitacion.Aprobada)
            || RolActual == Rol.SuperAdmin);

        b.Entity<Vacante>().HasQueryFilter(v =>
               (EmpresaActualId != null && v.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Estudiante
                && v.Estado == EstadoVacante.Publicada
                && v.Empresa.Activa
                && v.Empresa.Habilitaciones.Any(h => h.UniversidadId == UniversidadActualId && h.Estado == EstadoHabilitacion.Aprobada))
            || (RolActual == Rol.Coordinador
                && v.Empresa.Habilitaciones.Any(h => h.UniversidadId == UniversidadActualId)));

        // Tablas puente: heredan la visibilidad de su vacante (D-22).
        b.Entity<VacanteCarrera>().HasQueryFilter(x => Vacantes.Any(v => v.Id == x.VacanteId));
        b.Entity<VacanteHabilidad>().HasQueryFilter(x => Vacantes.Any(v => v.Id == x.VacanteId));

        b.Entity<Postulacion>().HasQueryFilter(p =>
               (EmpresaActualId != null && p.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Estudiante && p.EstudianteId == UsuarioActualId)
            || (RolActual == Rol.Coordinador && p.Estudiante.UniversidadId == UniversidadActualId));

        b.Entity<HistorialPostulacion>().HasQueryFilter(h =>
               (EmpresaActualId != null && h.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Estudiante && h.Postulacion.EstudianteId == UsuarioActualId)
            || (RolActual == Rol.Coordinador && h.Postulacion.Estudiante.UniversidadId == UniversidadActualId));

        // La empresa solo ve el CV que se le adjuntó en una postulación (HU-E06).
        b.Entity<Curriculum>().HasQueryFilter(c =>
               (RolActual == Rol.Estudiante && c.EstudianteId == UsuarioActualId)
            || (EmpresaActualId != null && Postulaciones.Any(p => p.CurriculumId == c.Id && p.EmpresaId == EmpresaActualId)));

        // La empresa solo ve el perfil de quien se postuló a ella (S3.2).
        b.Entity<PerfilEstudiante>().HasQueryFilter(p =>
               (RolActual == Rol.Estudiante && p.UsuarioId == UsuarioActualId)
            || (RolActual == Rol.Coordinador && p.Usuario.UniversidadId == UniversidadActualId)
            || (EmpresaActualId != null && Postulaciones.Any(x => x.EstudianteId == p.UsuarioId && x.EmpresaId == EmpresaActualId)));

        // Sprint 2: la práctica y sus hijas siguen la misma regla que la postulación de origen.
        b.Entity<Practica>().HasQueryFilter(x =>
               (EmpresaActualId != null && x.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Estudiante && x.Postulacion.EstudianteId == UsuarioActualId)
            || (RolActual == Rol.Coordinador && x.Postulacion.Estudiante.UniversidadId == UniversidadActualId));

        b.Entity<DocumentoPractica>().HasQueryFilter(x =>
               (EmpresaActualId != null && x.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Estudiante && x.Practica.Postulacion.EstudianteId == UsuarioActualId)
            || (RolActual == Rol.Coordinador && x.Practica.Postulacion.Estudiante.UniversidadId == UniversidadActualId));

        b.Entity<EvaluacionEmpresa>().HasQueryFilter(x =>
               (EmpresaActualId != null && x.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Estudiante && x.Practica.Postulacion.EstudianteId == UsuarioActualId)
            || (RolActual == Rol.Coordinador && x.Practica.Postulacion.Estudiante.UniversidadId == UniversidadActualId));

        b.Entity<EvaluacionPracticante>().HasQueryFilter(x =>
               (EmpresaActualId != null && x.EmpresaId == EmpresaActualId)
            || (RolActual == Rol.Estudiante && x.Practica.Postulacion.EstudianteId == UsuarioActualId)
            || (RolActual == Rol.Coordinador && x.Practica.Postulacion.Estudiante.UniversidadId == UniversidadActualId));
    }
}
