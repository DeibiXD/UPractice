import { z } from 'zod'

// Mismos textos y reglas que backend/.../Features/Perfil (MensajesPerfil y PerfilValidators) y docs/mensajes-validacion.md.
export const MENSAJE_NUMERO_CUENTA = 'El número de cuenta debe tener solo dígitos (6 a 20).'
export const MENSAJE_CARRERA = 'Seleccione una carrera válida.'
export const MENSAJE_INDICE = 'El índice debe estar entre 0 y 100.'

/** Máximo 2 decimales, igual que PrecisionScale(5, 2) del backend. */
const dosDecimales = (n: number) => Math.abs(n * 100 - Math.round(n * 100)) < 1e-9

export const esquemaPerfilAcademico = z.object({
  numeroCuenta: z.string().trim().regex(/^\d{6,20}$/, MENSAJE_NUMERO_CUENTA),
  carreraId: z.string().min(1, MENSAJE_CARRERA),
  // register(..., { valueAsNumber: true }): un campo vacío llega como NaN y debe dar el mismo mensaje.
  indiceAcademico: z
    .number({ error: MENSAJE_INDICE })
    .min(0, MENSAJE_INDICE)
    .max(100, MENSAJE_INDICE)
    .refine(dosDecimales, MENSAJE_INDICE),
})
export type PerfilAcademicoFormulario = z.infer<typeof esquemaPerfilAcademico>
