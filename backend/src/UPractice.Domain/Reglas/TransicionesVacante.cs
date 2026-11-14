using UPractice.Domain.Enums;
using UPractice.Domain.Excepciones;

namespace UPractice.Domain.Reglas;

/// <summary>
/// Máquina de estados de la vacante (§3.6 del diseño). La condición "empresa habilitada"
/// para publicar la revisa el handler (422 EMPRESA_NO_HABILITADA).
/// </summary>
public static class TransicionesVacante
{
    private static readonly HashSet<(EstadoVacante Desde, EstadoVacante Hacia)> Permitidas =
    [
        (EstadoVacante.Borrador, EstadoVacante.Publicada),
        (EstadoVacante.Publicada, EstadoVacante.Pausada),
        (EstadoVacante.Pausada, EstadoVacante.Publicada),
        (EstadoVacante.Publicada, EstadoVacante.Cerrada),
        (EstadoVacante.Pausada, EstadoVacante.Cerrada),
    ];

    public static bool EsValida(EstadoVacante desde, EstadoVacante hacia) => Permitidas.Contains((desde, hacia));

    public static void Validar(EstadoVacante desde, EstadoVacante hacia)
    {
        if (!EsValida(desde, hacia))
            throw new ReglaNegocioException(CodigosError.TransicionInvalida,
                $"No se puede pasar la vacante de {desde} a {hacia}.");
    }

    /// <summary>Solo se edita en Borrador o Pausada (HU-E04).</summary>
    public static bool SePuedeEditar(EstadoVacante estado) => estado is EstadoVacante.Borrador or EstadoVacante.Pausada;
}
