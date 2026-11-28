import { z } from 'zod'
import { esquemaCorreoRecortado, esquemaNombreObligatorio, esquemaNombreOpcional } from '@/shared/lib/validaciones'

// Mismos textos que backend/.../Features/Admin (MensajesCoordinadores, MensajesEmpresasAdmin) y docs/mensajes-validacion.md.
export const MENSAJE_UNIVERSIDAD = 'Seleccione una universidad.'
export const MENSAJE_MOTIVO = 'Indique el motivo (10 a 500 caracteres).'

/** HU-A03: simétrico con CrearCoordinadorValidator. */
export const esquemaCrearCoordinador = z.object({
  universidadId: z.string().min(1, MENSAJE_UNIVERSIDAD),
  nombre1: esquemaNombreObligatorio,
  nombre2: esquemaNombreOpcional,
  apellido1: esquemaNombreObligatorio,
  apellido2: esquemaNombreOpcional,
  correo: esquemaCorreoRecortado,
})
export type CrearCoordinadorFormulario = z.infer<typeof esquemaCrearCoordinador>

/** HU-A04: al suspender, el motivo es obligatorio (10 a 500 caracteres, sin espacios exteriores). */
export const esquemaSuspenderEmpresa = z.object({
  motivoSuspension: z.string().trim().min(10, MENSAJE_MOTIVO).max(500, MENSAJE_MOTIVO),
})
export type SuspenderEmpresaFormulario = z.infer<typeof esquemaSuspenderEmpresa>
