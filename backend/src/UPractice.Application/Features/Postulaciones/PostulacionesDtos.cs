using UPractice.Application.Features.Auth;
using UPractice.Domain.Enums;

namespace UPractice.Application.Features.Postulaciones;

// DTO de S5.6 (Postulaciones) tal como están en docs/contratos/api-types.ts.
// En el JSON salen en camelCase y los enums como texto.

public sealed record VacanteRef(Guid Id, string Titulo);

public sealed record CurriculumRef(Guid Id, string NombreVersion);

/// <summary>Lo que el estudiante puede hacer con su postulación (HU-06). El frontend pinta un botón por cada una.</summary>
public enum AccionPostulacion
{
    Retirar,
    Reactivar,
    AceptarOferta,
    DeclinarOferta,
}

// ===== Estudiante (HU-05, HU-06) =====

public sealed record CrearPostulacionRequest(Guid CurriculumId);

/// <summary>Puede elegir otro CV; se aplican las mismas validaciones que al postularse.</summary>
public sealed record ReactivarPostulacionRequest(Guid CurriculumId);

/// <summary>Nullable a propósito: si el cuerpo llega vacío no se debe interpretar como "declinar" (acción final).</summary>
public sealed record ResponderOfertaRequest(bool? Aceptar);

public record MiPostulacionDto(
    Guid Id,
    VacanteRef Vacante,
    Referencia Empresa,
    EstadoPostulacion Estado,
    DateTime FechaPostulacion,
    DateTime FechaUltimoCambio,   // fecha del último registro del historial (D-23)
    IReadOnlyList<AccionPostulacion> Acciones);

public record HistorialItemDto(
    EstadoPostulacion? EstadoAnterior,
    EstadoPostulacion EstadoNuevo,
    DateTime Fecha,
    string? Comentario);

public sealed record MiPostulacionDetalleDto(
    Guid Id,
    VacanteRef Vacante,
    Referencia Empresa,
    EstadoPostulacion Estado,
    DateTime FechaPostulacion,
    DateTime FechaUltimoCambio,
    IReadOnlyList<AccionPostulacion> Acciones,
    CurriculumRef Curriculum,
    IReadOnlyList<HistorialItemDto> Historial)
    : MiPostulacionDto(Id, Vacante, Empresa, Estado, FechaPostulacion, FechaUltimoCambio, Acciones);

// ===== Empresa (HU-E05, HU-E06, HU-E07) =====

public sealed record PostulanteEstudianteResumenDto(string NombreCompleto, string Universidad, string Carrera);

public sealed record PostulanteResumenDto(
    Guid PostulacionId,
    PostulanteEstudianteResumenDto Estudiante,
    EstadoPostulacion Estado,
    bool CumpleRequisitos,
    DateTime FechaPostulacion);

public sealed record PostulanteEstudianteDetalleDto(
    string NombreCompleto,
    string Correo,
    string Universidad,
    string Facultad,
    string Carrera,
    decimal? IndiceAcademico,   // null si el estudiante no autorizó mostrarlo (HU-02, MostrarIndice)
    IReadOnlyList<string> Habilidades,
    string VerificadoPor,   // nombre de la universidad que verificó el perfil
    DateTime FechaVerificacion);

public sealed record HistorialPostulanteDto(
    EstadoPostulacion? EstadoAnterior,
    EstadoPostulacion EstadoNuevo,
    DateTime Fecha,
    string? Comentario,
    string CambiadoPor)
    : HistorialItemDto(EstadoAnterior, EstadoNuevo, Fecha, Comentario);

public sealed record PostulanteDetalleDto(
    Guid PostulacionId,
    VacanteRef Vacante,
    EstadoPostulacion Estado,
    bool CumpleRequisitos,
    DateTime FechaPostulacion,
    PostulanteEstudianteDetalleDto Estudiante,
    CurriculumRef Curriculum,
    IReadOnlyList<HistorialPostulanteDto> Historial);

/// <summary>Solo se aceptan EnRevision, Entrevista, Oferta y Rechazada; cualquier otro destino lo rechaza la máquina de estados (422).</summary>
public sealed record CambiarEstadoPostulacionRequest(EstadoPostulacion NuevoEstado, string? Comentario);
