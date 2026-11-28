import { describe, expect, it } from 'vitest'
import type { EstadoPostulacion } from '@/shared/api/types'
import { ESTADOS_POSTULACION, ETIQUETA_ESTADO, siguientesEmpresa } from './transiciones'

// Tabla copiada de TransicionesPostulacion.cs (actor Empresa).
const BACKEND: Record<EstadoPostulacion, string[]> = {
  Enviada: ['EnRevision', 'Entrevista', 'Rechazada'],
  EnRevision: ['Entrevista', 'Oferta', 'Rechazada'],
  Entrevista: ['Oferta', 'Rechazada'],
  Oferta: [],
  Aceptada: [],
  Declinada: [],
  Rechazada: [],
  Retirada: [],
}

describe('siguientesEmpresa', () => {
  it('coincide con la máquina de estados del backend para cada estado', () => {
    for (const estado of ESTADOS_POSTULACION) {
      expect(siguientesEmpresa(estado), estado).toEqual(BACKEND[estado])
    }
  })

  it('la oferta ya no la mueve la empresa: responde el estudiante', () => {
    expect(siguientesEmpresa('Oferta')).toEqual([])
  })
})

describe('ETIQUETA_ESTADO', () => {
  it('tiene texto para todos los estados', () => {
    for (const estado of ESTADOS_POSTULACION) expect(ETIQUETA_ESTADO[estado].length).toBeGreaterThan(0)
    expect(ETIQUETA_ESTADO.EnRevision).toBe('En revisión')
  })
})
