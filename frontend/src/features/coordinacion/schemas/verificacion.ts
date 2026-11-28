import { z } from 'zod'

// Mismo texto que MensajesVerificacion.Observacion (backend) y docs/mensajes-validacion.md.
export const MENSAJE_OBSERVACION = 'Indique la observación (10 a 500 caracteres).'

/** Rechazo de un perfil: la observación es obligatoria (10 a 500 caracteres, sin espacios exteriores). */
export const esquemaRechazoVerificacion = z.object({
  observacion: z.string().trim().min(10, MENSAJE_OBSERVACION).max(500, MENSAJE_OBSERVACION),
})
export type RechazoVerificacionFormulario = z.infer<typeof esquemaRechazoVerificacion>
