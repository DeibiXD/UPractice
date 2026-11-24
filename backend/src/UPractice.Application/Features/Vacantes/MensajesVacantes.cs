namespace UPractice.Application.Features.Vacantes;

/// <summary>Textos del contrato compartido en docs/mensajes-validacion.md.</summary>
public static class MensajesVacantes
{
    public const string Titulo = "El título debe tener entre 5 y 120 caracteres.";
    public const string Descripcion = "La descripción debe tener entre 30 y 4000 caracteres.";
    public const string Ubicacion = "Debe tener entre 2 y 120 caracteres.";
    public const string Modalidad = "Seleccione una modalidad.";
    public const string Cupos = "Los cupos deben estar entre 1 y 50.";
    public const string IndiceMinimo = "El índice mínimo debe estar entre 0 y 100.";
    public const string FechaCierre = "La fecha de cierre debe ser posterior a hoy.";
    public const string CarrerasRequeridas = "Seleccione al menos una carrera.";
    public const string CarrerasInvalidas = "Seleccione carreras válidas y activas.";
    public const string CarrerasDuplicadas = "No repita carreras.";
    public const string HabilidadesInvalidas = "Seleccione habilidades válidas.";
    public const string HabilidadesDuplicadas = "No repita habilidades.";
    public const string Estado = "Seleccione un estado de vacante válido.";
    public const string Pagina = "La página debe ser mayor o igual a 1.";
    public const string TamanoPagina = "El tamaño de página debe estar entre 1 y 50.";
}
