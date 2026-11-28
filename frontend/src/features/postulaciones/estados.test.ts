import { describe, expect, it } from 'vitest'
import type { EstadoPostulacion } from '@/shared/api/types'
import {
  curriculumEfectivo,
  ESTADOS_FILTRO,
  ETIQUETA_ESTADO,
  formatearFechaHora,
  ordenarAcciones,
} from './estados'

const TODOS: EstadoPostulacion[] = [
  'Enviada',
  'EnRevision',
  'Entrevista',
  'Oferta',
  'Aceptada',
  'Declinada',
  'Rechazada',
  'Retirada',
]

describe('estados de postulación', () => {
  it('cada estado tiene etiqueta y aparece una sola vez en el filtro', () => {
    for (const estado of TODOS) {
      expect(ETIQUETA_ESTADO[estado]).toBeTruthy()
    }
    expect([...ESTADOS_FILTRO].sort()).toEqual([...TODOS].sort())
  })

  it('ordena las acciones y conserva solo las que envió la API', () => {
    expect(ordenarAcciones(['Retirar', 'DeclinarOferta', 'AceptarOferta'])).toEqual([
      'AceptarOferta',
      'DeclinarOferta',
      'Retirar',
    ])
    expect(ordenarAcciones([])).toEqual([])
  })

  it('formatea en horario de Honduras (UTC-6)', () => {
    // 03:30 UTC del 5 de noviembre es el 4 de noviembre a las 21:30 en Honduras.
    expect(formatearFechaHora('2026-11-05T03:30:00Z')).toContain('04')
    expect(formatearFechaHora(null)).toBe('—')
    expect(formatearFechaHora('no-es-fecha')).toBe('—')
  })
})

describe('curriculumEfectivo', () => {
  const lista = [
    { id: 'a', esPrincipal: false },
    { id: 'b', esPrincipal: true },
  ]
  it('usa el elegido si existe, si no el principal, si no el primero', () => {
    expect(curriculumEfectivo('a', lista)).toBe('a')
    expect(curriculumEfectivo(null, lista)).toBe('b')
    expect(curriculumEfectivo('borrado', lista)).toBe('b')
    expect(curriculumEfectivo(null, [{ id: 'x', esPrincipal: false }])).toBe('x')
    expect(curriculumEfectivo(null, [])).toBeNull()
  })
})
