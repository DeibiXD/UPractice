import { z } from 'zod'
import { esquemaCorreo, esquemaPasswordNueva, MENSAJE_CONFIRMAR } from '@/shared/lib/validaciones'

// Mismos textos que backend/.../Features/Auth/Validadores.cs

export const esquemaLogin = z.object({
  correo: esquemaCorreo,
  password: z.string().min(1, 'Ingrese su contraseña.'),
})
export type LoginFormulario = z.infer<typeof esquemaLogin>

export const esquemaCambiarPassword = z
  .object({
    passwordActual: z.string().min(1, 'Ingrese su contraseña actual.'),
    passwordNuevo: esquemaPasswordNueva,
    confirmarPassword: z.string(),
  })
  .refine((datos) => datos.passwordNuevo === datos.confirmarPassword, {
    message: MENSAJE_CONFIRMAR,
    path: ['confirmarPassword'],
  })
export type CambiarPasswordFormulario = z.infer<typeof esquemaCambiarPassword>
