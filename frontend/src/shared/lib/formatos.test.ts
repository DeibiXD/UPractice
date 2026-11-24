import { describe, expect, it } from 'vitest'
import { formatearFecha, formatearFechaHora } from './formatos'

describe('formatearFecha', () => {
  it('una fecha sin hora no se corre un día por la zona horaria', () => {
    const texto = formatearFecha('2026-12-15')
    expect(texto).toContain('15')
    expect(texto).toContain('2026')
  })

  it('una fecha ISO se muestra en hora de Honduras (UTC-6)', () => {
    // 2026-12-16T03:00:00Z son las 9 p. m. del 15 en Honduras.
    expect(formatearFecha('2026-12-16T03:00:00Z')).toContain('15')
  })

  it('valores vacíos o inválidos muestran una raya', () => {
    expect(formatearFecha(null)).toBe('—')
    expect(formatearFecha(undefined)).toBe('—')
    expect(formatearFecha('')).toBe('—')
    expect(formatearFecha('no es fecha')).toBe('—')
  })
})

describe('formatearFechaHora', () => {
  it('incluye fecha y hora', () => {
    const texto = formatearFechaHora('2026-12-15T21:30:00Z')
    expect(texto).toContain('15')
    expect(texto).toMatch(/\d{1,2}:30/)
  })

  it('valores vacíos o inválidos muestran una raya', () => {
    expect(formatearFechaHora(null)).toBe('—')
    expect(formatearFechaHora('xx')).toBe('—')
  })
})
