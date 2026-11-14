namespace UPractice.Domain.Excepciones;

/// <summary>
/// Códigos de error de negocio (§5.4 del diseño). El frontend los recibe en el campo <c>code</c>
/// del ProblemDetails, así que el texto debe coincidir exactamente. Usar siempre estas constantes,
/// nunca escribir el código a mano en un handler.
/// </summary>
public static class CodigosError
{
    // ----- 409 · ConflictoException -----
    public const string CorreoEnUso = "CORREO_EN_USO";
    public const string RtnEnUso = "RTN_EN_USO";
    public const string PostulacionDuplicada = "POSTULACION_DUPLICADA";
    public const string SiglasEnUso = "SIGLAS_EN_USO";
    public const string DominioEnUso = "DOMINIO_EN_USO";

    // ----- 422 · ReglaNegocioException -----
    public const string CorreoNoInstitucional = "CORREO_NO_INSTITUCIONAL";
    public const string PerfilNoVerificado = "PERFIL_NO_VERIFICADO";
    public const string PerfilVerificadoInmutable = "PERFIL_VERIFICADO_INMUTABLE";
    public const string CarreraNoElegible = "CARRERA_NO_ELEGIBLE";
    public const string VacanteNoDisponible = "VACANTE_NO_DISPONIBLE";
    public const string PracticaYaAceptada = "PRACTICA_YA_ACEPTADA";
    public const string TransicionInvalida = "TRANSICION_INVALIDA";
    public const string SinCupos = "SIN_CUPOS";
    public const string EmpresaNoHabilitada = "EMPRESA_NO_HABILITADA";
    public const string LimiteCurriculums = "LIMITE_CURRICULUMS";
    public const string CarreraNoPermitida = "CARRERA_NO_PERMITIDA";

    // ----- 403 · lo emite el filtro de autorización (D-17) -----
    public const string CambioPasswordRequerido = "CAMBIO_PASSWORD_REQUERIDO";
}
