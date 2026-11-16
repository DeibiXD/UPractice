using Microsoft.EntityFrameworkCore;
using UPractice.Application.Abstracciones;
using UPractice.Application.Features.Auth;
using UPractice.Domain.Entidades;
using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Application.Features.Postulaciones;

/// <summary>
/// Lecturas compartidas por los handlers del estudiante y de la empresa. Se proyecta a tipos anónimos y el nombre
/// completo se arma en memoria porque NombreCompleto es una propiedad calculada que EF no puede traducir a SQL.
/// </summary>
internal static class ConsultasPostulacion
{
    // ===== Estudiante =====

    public static async Task<List<MiPostulacionDto>> ListarMiasAsync(IAppDbContext db, CancellationToken ct)
    {
        // El filtro global del estudiante ya deja solo sus postulaciones.
        var filas = await db.Postulaciones.AsNoTracking()
            .Select(p => new
            {
                p.Id,
                VacanteId = p.VacanteId,
                Titulo = p.Vacante.Titulo,
                EmpresaId = p.EmpresaId,
                Empresa = p.Empresa.NombreComercial,
                p.Estado,
                p.FechaPostulacion,
                UltimoCambio = p.Historial.Max(h => (DateTime?)h.Fecha),
                VacanteEstado = p.Vacante.Estado,
                p.Vacante.FechaCierre,
            })
            .ToListAsync(ct);

        var tienePracticaAceptada = filas.Any(f => f.Estado == EstadoPostulacion.Aceptada);

        return filas
            .Select(f => new MiPostulacionDto(
                f.Id,
                new VacanteRef(f.VacanteId, f.Titulo),
                new Referencia(f.EmpresaId, f.Empresa),
                f.Estado,
                f.FechaPostulacion,
                f.UltimoCambio ?? f.FechaPostulacion,
                ReglasPostulacion.AccionesPara(f.Estado, ReglasPostulacion.VacanteVigente(f.VacanteEstado, f.FechaCierre) && !tienePracticaAceptada)))
            .OrderByDescending(p => p.FechaUltimoCambio)
            .ToList();
    }

    public static async Task<MiPostulacionDetalleDto> DetalleMioAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var fila = await db.Postulaciones.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                VacanteId = p.VacanteId,
                Titulo = p.Vacante.Titulo,
                EmpresaId = p.EmpresaId,
                Empresa = p.Empresa.NombreComercial,
                p.Estado,
                p.FechaPostulacion,
                VacanteEstado = p.Vacante.Estado,
                p.Vacante.FechaCierre,
                CurriculumId = p.CurriculumId,
                CurriculumNombre = p.Curriculum.NombreVersion,
                Historial = p.Historial
                    .OrderBy(h => h.Fecha)
                    .Select(h => new { h.EstadoAnterior, h.EstadoNuevo, h.Fecha, h.Comentario })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("La postulación no existe.");

        var tienePracticaAceptada = await db.Postulaciones.AnyAsync(p => p.Estado == EstadoPostulacion.Aceptada, ct);
        var historial = fila.Historial.Select(h => new HistorialItemDto(h.EstadoAnterior, h.EstadoNuevo, h.Fecha, h.Comentario)).ToList();

        return new MiPostulacionDetalleDto(
            fila.Id,
            new VacanteRef(fila.VacanteId, fila.Titulo),
            new Referencia(fila.EmpresaId, fila.Empresa),
            fila.Estado,
            fila.FechaPostulacion,
            historial.Count > 0 ? historial.Max(h => h.Fecha) : fila.FechaPostulacion,
            ReglasPostulacion.AccionesPara(fila.Estado, ReglasPostulacion.VacanteVigente(fila.VacanteEstado, fila.FechaCierre) && !tienePracticaAceptada),
            new CurriculumRef(fila.CurriculumId, fila.CurriculumNombre),
            historial);
    }

    // ===== Empresa =====

    public static async Task<List<PostulanteResumenDto>> ListarPostulantesAsync(IAppDbContext db, Guid vacanteId, CancellationToken ct)
    {
        // Si la vacante es de otra empresa el filtro global la oculta: 404, no una lista vacía (no revela que existe).
        if (!await db.Vacantes.AnyAsync(v => v.Id == vacanteId, ct))
            throw new NoEncontradoException("La vacante no existe.");

        var filas = await db.Postulaciones.AsNoTracking()
            .Where(p => p.VacanteId == vacanteId)
            .Select(p => new
            {
                p.Id,
                p.Estudiante.Nombre1,
                p.Estudiante.Nombre2,
                p.Estudiante.Apellido1,
                p.Estudiante.Apellido2,
                Universidad = p.Estudiante.Universidad!.Nombre,
                Carrera = p.Estudiante.Perfil!.Carrera.Nombre,
                p.Estado,
                p.CumpleRequisitos,
                p.FechaPostulacion,
            })
            .OrderByDescending(p => p.FechaPostulacion)
            .ToListAsync(ct);

        return filas
            .Select(f => new PostulanteResumenDto(
                f.Id,
                new PostulanteEstudianteResumenDto(Usuario.ArmarNombreCompleto(f.Nombre1, f.Nombre2, f.Apellido1, f.Apellido2), f.Universidad, f.Carrera),
                f.Estado,
                f.CumpleRequisitos,
                f.FechaPostulacion))
            .ToList();
    }

    public static async Task<PostulanteDetalleDto> DetallePostulanteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var fila = await db.Postulaciones.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                VacanteId = p.VacanteId,
                Titulo = p.Vacante.Titulo,
                p.Estado,
                p.CumpleRequisitos,
                p.FechaPostulacion,
                p.Estudiante.Nombre1,
                p.Estudiante.Nombre2,
                p.Estudiante.Apellido1,
                p.Estudiante.Apellido2,
                p.Estudiante.Correo,
                Universidad = p.Estudiante.Universidad!.Nombre,
                Facultad = p.Estudiante.Perfil!.Carrera.Facultad.Nombre,
                Carrera = p.Estudiante.Perfil!.Carrera.Nombre,
                // Privacidad (HU-02): el índice solo se muestra si el estudiante lo autorizó.
                Indice = p.Estudiante.Perfil!.MostrarIndice ? (decimal?)p.Estudiante.Perfil!.IndiceAcademico : null,
                Habilidades = p.Estudiante.Habilidades.Select(h => h.Habilidad.Nombre).OrderBy(n => n).ToList(),
                FechaVerificacion = p.Estudiante.Perfil!.FechaVerificacion,
                CurriculumId = p.CurriculumId,
                CurriculumNombre = p.Curriculum.NombreVersion,
                Historial = p.Historial
                    .OrderBy(h => h.Fecha)
                    .Select(h => new
                    {
                        h.EstadoAnterior,
                        h.EstadoNuevo,
                        h.Fecha,
                        h.Comentario,
                        Nombre1 = h.CambiadoPor.Nombre1,
                        Nombre2 = h.CambiadoPor.Nombre2,
                        Apellido1 = h.CambiadoPor.Apellido1,
                        Apellido2 = h.CambiadoPor.Apellido2,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NoEncontradoException("La postulación no existe.");

        return new PostulanteDetalleDto(
            fila.Id,
            new VacanteRef(fila.VacanteId, fila.Titulo),
            fila.Estado,
            fila.CumpleRequisitos,
            fila.FechaPostulacion,
            new PostulanteEstudianteDetalleDto(
                Usuario.ArmarNombreCompleto(fila.Nombre1, fila.Nombre2, fila.Apellido1, fila.Apellido2),
                fila.Correo,
                fila.Universidad,
                fila.Facultad,
                fila.Carrera,
                fila.Indice,
                fila.Habilidades,
                fila.Universidad,
                fila.FechaVerificacion ?? default),
            new CurriculumRef(fila.CurriculumId, fila.CurriculumNombre),
            fila.Historial
                .Select(h => new HistorialPostulanteDto(
                    h.EstadoAnterior, h.EstadoNuevo, h.Fecha, h.Comentario,
                    Usuario.ArmarNombreCompleto(h.Nombre1, h.Nombre2, h.Apellido1, h.Apellido2)))
                .ToList());
    }
}
