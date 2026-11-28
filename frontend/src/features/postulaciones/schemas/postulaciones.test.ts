import { describe, expect, it } from 'vitest'
import { esquemaCambioEstado, esquemaSeleccionCurriculum } from './postulaciones'

function mensaje(resultado: { error?: { issues: { message: string }[] } }) {
  return resultado.error?.issues[0]?.message
}

describe('esquemaSeleccionCurriculum', () => {
  it('exige elegir un currículum', () => {
    expect(mensaje(esquemaSeleccionCurriculum.safeParse({ curriculumId: '' }))).toBe('Seleccione un currículum.')
    expect(esquemaSeleccionCurriculum.safeParse({ curriculumId: 'abc' }).success).toBe(true)
  })
})

describe('esquemaCambioEstado', () => {
  it('exige el nuevo estado', () => {
    expect(mensaje(esquemaCambioEstado.safeParse({ nuevoEstado: '', comentario: '' }))).toBe(
      'Seleccione el nuevo estado.',
    )
  })

  it('el comentario es opcional y llega hasta 500 caracteres', () => {
    expect(esquemaCambioEstado.safeParse({ nuevoEstado: 'Oferta', comentario: '' }).success).toBe(true)
    expect(esquemaCambioEstado.safeParse({ nuevoEstado: 'Oferta', comentario: 'a'.repeat(500) }).success).toBe(true)
    expect(mensaje(esquemaCambioEstado.safeParse({ nuevoEstado: 'Oferta', comentario: 'a'.repeat(501) }))).toBe(
      'Máximo 500 caracteres.',
    )
  })
})
