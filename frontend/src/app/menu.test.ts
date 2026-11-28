import { describe, expect, it } from 'vitest'
import { menuParaRol } from '@/shared/navegacion/menu'
import { menu } from './menu'

describe('menú de la aplicación', () => {
  it('el estudiante ve Vacantes y Mis postulaciones, en ese orden', () => {
    expect(menuParaRol(menu, 'Estudiante').map((i) => i.etiqueta)).toEqual(['Vacantes', 'Mis postulaciones'])
  })

  it('los usuarios de empresa ven Postulantes', () => {
    for (const rol of ['AdminEmpresa', 'Reclutador'] as const) {
      expect(menuParaRol(menu, rol).map((i) => i.etiqueta)).toContain('Postulantes')
    }
  })

  it('cada ruta del menú es única y empieza con "/"', () => {
    const rutas = menu.map((i) => i.ruta)
    expect(new Set(rutas).size).toBe(rutas.length)
    for (const ruta of rutas) expect(ruta.startsWith('/')).toBe(true)
  })
})
