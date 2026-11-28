import { describe, expect, it } from 'vitest'
import { MENSAJE_CORREO, MENSAJE_NOMBRE_OBLIGATORIO, MENSAJE_NOMBRE_OPCIONAL } from '@/shared/lib/validaciones'
import { esquemaCrearCoordinador, esquemaSuspenderEmpresa, MENSAJE_MOTIVO, MENSAJE_UNIVERSIDAD } from './admin'

const valido = { universidadId: 'u1', nombre1: 'Rosa', nombre2: '', apellido1: 'Meléndez', apellido2: '', correo: 'rosa@unah.hn' }

function errores(datos: unknown): Record<string, string> {
  const r = esquemaCrearCoordinador.safeParse(datos)
  if (r.success) return {}
  return Object.fromEntries(r.error.issues.map((i) => [String(i.path[0]), i.message]))
}

describe('esquemaCrearCoordinador (simétrico con CrearCoordinadorValidator)', () => {
  it('acepta acentos, apóstrofos y guiones y recorta espacios', () => {
    const r = esquemaCrearCoordinador.parse({
      ...valido, nombre1: '  María José ', apellido1: "O'Neil-Núñez", correo: '  rosa@unah.hn ',
    })
    expect(r.nombre1).toBe('María José')
    expect(r.correo).toBe('rosa@unah.hn')
  })

  it('reporta cada campo con el mensaje del backend', () => {
    expect(errores({ universidadId: '', nombre1: 'R', nombre2: 'Ana1', apellido1: '', apellido2: '', correo: 'no-es-correo' })).toEqual({
      universidadId: MENSAJE_UNIVERSIDAD,
      nombre1: MENSAJE_NOMBRE_OBLIGATORIO,
      nombre2: MENSAJE_NOMBRE_OPCIONAL,
      apellido1: MENSAJE_NOMBRE_OBLIGATORIO,
      correo: MENSAJE_CORREO,
    })
  })

  it.each(['Ana2', '-Ana', 'x'.repeat(51)])('rechaza el nombre "%s"', (nombre1) => {
    expect(errores({ ...valido, nombre1 }).nombre1).toBe(MENSAJE_NOMBRE_OBLIGATORIO)
  })
})

describe('esquemaSuspenderEmpresa (simétrico con CambiarEstadoEmpresaValidator al suspender)', () => {
  it.each(['', 'corto', '   nueve   ', 'x'.repeat(501)])('rechaza el motivo "%s"', (motivoSuspension) => {
    const r = esquemaSuspenderEmpresa.safeParse({ motivoSuspension })
    expect(r.success).toBe(false)
    expect(r.error?.issues[0].message).toBe(MENSAJE_MOTIVO)
  })

  it('acepta 10 a 500 caracteres', () => {
    expect(esquemaSuspenderEmpresa.parse({ motivoSuspension: ' Publicó vacantes falsas. ' }).motivoSuspension).toBe('Publicó vacantes falsas.')
  })
})
