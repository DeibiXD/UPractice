namespace UPractice.Domain.Enums;

/// <summary>Estados de la postulación (§3.6). Finales: Aceptada, Declinada, Rechazada; Retirada solo se reactiva por el estudiante (D-20).</summary>
public enum EstadoPostulacion
{
    Enviada,
    EnRevision,
    Entrevista,
    Oferta,
    Aceptada,
    Declinada,
    Rechazada,
    Retirada,
}
