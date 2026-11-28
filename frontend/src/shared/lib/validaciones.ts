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

export const MENSAJE_NOMBRE_OBLIGATORIO = 'Debe tener entre 2 y 50 letras.'
export const MENSAJE_NOMBRE_OPCIONAL = 'Máximo 50 letras.'

export const esquemaCorreo = z.string().max(150, MENSAJE_CORREO).pipe(z.email(MENSAJE_CORREO))

/** Correo sin espacios exteriores (el backend lo recorta y lo guarda en minúsculas). */
export const esquemaCorreoRecortado = z.string().trim().pipe(esquemaCorreo)

// Igual que ReglasValidacion.NombreObligatorio/NombreOpcional (D-23): letras (con acentos), espacios, apóstrofo y guion.
const SOLO_LETRAS = /^[\p{L}][\p{L} '-]*$/u

/** nombre1 y apellido1: 2 a 50, solo letras; se valida sin espacios exteriores. */
export const esquemaNombreObligatorio = z
  .string()
  .trim()
  .min(2, MENSAJE_NOMBRE_OBLIGATORIO)
  .max(50, MENSAJE_NOMBRE_OBLIGATORIO)
  .regex(SOLO_LETRAS, MENSAJE_NOMBRE_OBLIGATORIO)

/** nombre2 y apellido2: opcionales, máximo 50, solo letras. Vacío es válido. */
export const esquemaNombreOpcional = z
  .string()
  .trim()
  .max(50, MENSAJE_NOMBRE_OPCIONAL)
  .refine((v) => v === '' || SOLO_LETRAS.test(v), MENSAJE_NOMBRE_OPCIONAL)

export const esquemaPasswordNueva = z
  .string()
  .min(8, MENSAJE_PASSWORD)
  .max(64, MENSAJE_PASSWORD)
  .regex(/[A-Z]/, MENSAJE_PASSWORD)
  .regex(/[a-z]/, MENSAJE_PASSWORD)
  .regex(/[0-9]/, MENSAJE_PASSWORD)
