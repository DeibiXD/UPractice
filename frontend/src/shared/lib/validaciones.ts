import { z } from 'zod'

/**
 * Reglas y mensajes compartidos. Deben ser IDÉNTICOS, carácter por carácter, a
 * backend/src/UPractice.Application/Comun/ReglasValidacion.cs y a docs/mensajes-validacion.md.
 * Si cambia un mensaje, cambia en los tres lugares en el mismo PR.
 */
export const MENSAJE_CORREO = 'Ingrese un correo válido.'
export const MENSAJE_PASSWORD =
  'La contraseña debe tener 8 a 64 caracteres, con mayúscula, minúscula y número.'
export const MENSAJE_CONFIRMAR = 'Las contraseñas no coinciden.'

export const esquemaCorreo = z.string().max(150, MENSAJE_CORREO).pipe(z.email(MENSAJE_CORREO))

export const esquemaPasswordNueva = z
  .string()
  .min(8, MENSAJE_PASSWORD)
  .max(64, MENSAJE_PASSWORD)
  .regex(/[A-Z]/, MENSAJE_PASSWORD)
  .regex(/[a-z]/, MENSAJE_PASSWORD)
  .regex(/[0-9]/, MENSAJE_PASSWORD)
