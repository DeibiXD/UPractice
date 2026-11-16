import { describe, expect, it } from 'vitest'
import { esquemaCambiarPassword, esquemaLogin } from './auth'

// Los mensajes deben ser idénticos a los de backend/.../Features/Auth/Validadores.cs
// y a docs/mensajes-validacion.md.

const MENSAJE_PASSWORD = 'La contraseña debe tener 8 a 64 caracteres, con mayúscula, minúscula y número.'

function mensajes(resultado: { success: boolean; error?: { issues: { path: PropertyKey[]; message: string }[] } }) {
  return Object.fromEntries((resultado.error?.issues ?? []).map((i) => [String(i.path[0]), i.message]))
}

describe('esquemaLogin', () => {
  it('acepta un correo y una contraseña', () => {
    expect(esquemaLogin.safeParse({ correo: 'ana@unah.hn', password: 'x' }).success).toBe(true)
  })

  it('correo vacío, sin formato o de más de 150 caracteres', () => {
    for (const correo of ['', 'no-es-correo', `${'a'.repeat(150)}@unah.hn`]) {
      expect(mensajes(esquemaLogin.safeParse({ correo, password: 'x' })).correo).toBe('Ingrese un correo válido.')
    }
  })

  it('contraseña requerida', () => {
    expect(mensajes(esquemaLogin.safeParse({ correo: 'ana@unah.hn', password: '' })).password).toBe(
      'Ingrese su contraseña.',
    )
  })
})

describe('esquemaCambiarPassword', () => {
  const valido = { passwordActual: 'Temporal-123', passwordNuevo: 'Nueva-12345', confirmarPassword: 'Nueva-12345' }

  it('acepta datos válidos', () => {
    expect(esquemaCambiarPassword.safeParse(valido).success).toBe(true)
  })

  it('la contraseña nueva exige 8 a 64, mayúscula, minúscula y número', () => {
    for (const passwordNuevo of ['Ab1', 'sinmayuscula1', 'SINMINUSCULA1', 'SinNumeroAqui', `Aa1${'x'.repeat(62)}`]) {
      const resultado = esquemaCambiarPassword.safeParse({ ...valido, passwordNuevo, confirmarPassword: passwordNuevo })
      expect(mensajes(resultado).passwordNuevo).toBe(MENSAJE_PASSWORD)
    }
  })

  it('la confirmación debe coincidir y el error se pinta en confirmarPassword', () => {
    const resultado = esquemaCambiarPassword.safeParse({ ...valido, confirmarPassword: 'Otra-12345' })
    expect(mensajes(resultado).confirmarPassword).toBe('Las contraseñas no coinciden.')
  })

  it('la contraseña actual es requerida', () => {
    expect(mensajes(esquemaCambiarPassword.safeParse({ ...valido, passwordActual: '' })).passwordActual).toBe(
      'Ingrese su contraseña actual.',
    )
  })
})
