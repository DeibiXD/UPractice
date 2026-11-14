using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Domain.Reglas;

/// <summary>Quién ejecuta la transición. La empresa incluye Reclutador y AdminEmpresa.</summary>
public enum ActorPostulacion
{
    Estudiante,
    Empresa,
}

/// <summary>
/// Máquina de estados de la postulación (S3.6 del diseño). Solo valida que el salto exista y que
/// lo haga el actor correcto. Las demás condiciones (perfil verificado, vacante vigente, cupos,
/// oferta ya aceptada) las revisa el handler, porque necesitan la base de datos.
/// </summary>
public static class TransicionesPostulacion
{
    private static readonly Dictionary<(EstadoPostulacion Desde, EstadoPostulacion Hacia), ActorPostulacion> Permitidas = new()
    {
        [(EstadoPostulacion.Enviada, EstadoPostulacion.EnRevision)] = ActorPostulacion.Empresa,
        [(EstadoPostulacion.Enviada, EstadoPostulacion.Entrevista)] = ActorPostulacion.Empresa,
        [(EstadoPostulacion.EnRevision, EstadoPostulacion.Entrevista)] = ActorPostulacion.Empresa,
        [(EstadoPostulacion.EnRevision, EstadoPostulacion.Oferta)] = ActorPostulacion.Empresa,
        [(EstadoPostulacion.Entrevista, EstadoPostulacion.Oferta)] = ActorPostulacion.Empresa,
        [(EstadoPostulacion.Enviada, EstadoPostulacion.Rechazada)] = ActorPostulacion.Empresa,
        [(EstadoPostulacion.EnRevision, EstadoPostulacion.Rechazada)] = ActorPostulacion.Empresa,
        [(EstadoPostulacion.Entrevista, EstadoPostulacion.Rechazada)] = ActorPostulacion.Empresa,

        [(EstadoPostulacion.Oferta, EstadoPostulacion.Aceptada)] = ActorPostulacion.Estudiante,
        [(EstadoPostulacion.Oferta, EstadoPostulacion.Declinada)] = ActorPostulacion.Estudiante,
        [(EstadoPostulacion.Enviada, EstadoPostulacion.Retirada)] = ActorPostulacion.Estudiante,
        [(EstadoPostulacion.EnRevision, EstadoPostulacion.Retirada)] = ActorPostulacion.Estudiante,
        [(EstadoPostulacion.Entrevista, EstadoPostulacion.Retirada)] = ActorPostulacion.Estudiante,
        [(EstadoPostulacion.Retirada, EstadoPostulacion.Enviada)] = ActorPostulacion.Estudiante, // reactivar (D-20)
    };

    /// <summary>Estados en los que la postulación sigue en el embudo (se retiran al aceptar otra oferta).</summary>
    public static readonly IReadOnlySet<EstadoPostulacion> Activos = new HashSet<EstadoPostulacion>
    {
        EstadoPostulacion.Enviada, EstadoPostulacion.EnRevision, EstadoPostulacion.Entrevista, EstadoPostulacion.Oferta,
    };

    public static bool EsValida(EstadoPostulacion desde, EstadoPostulacion hacia, ActorPostulacion actor)
        => Permitidas.TryGetValue((desde, hacia), out var permitido) && permitido == actor;

    /// <summary>Lanza 422 TRANSICION_INVALIDA si el salto no está permitido para ese actor.</summary>
    public static void Validar(EstadoPostulacion desde, EstadoPostulacion hacia, ActorPostulacion actor)
    {
        if (!EsValida(desde, hacia, actor))
            throw new ReglaNegocioException(CodigosError.TransicionInvalida,
                $"No se puede pasar la postulación de {desde} a {hacia}.");
    }

    /// <summary>Destinos posibles para un actor. Sirve para calcular <c>acciones</c> en MiPostulacionDto (HU-06).</summary>
    public static IEnumerable<EstadoPostulacion> Siguientes(EstadoPostulacion desde, ActorPostulacion actor)
        => Permitidas.Where(p => p.Key.Desde == desde && p.Value == actor).Select(p => p.Key.Hacia);
}
