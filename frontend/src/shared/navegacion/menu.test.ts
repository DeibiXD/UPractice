import { describe, expect, it } from 'vitest'
import { menuParaRol, type ItemMenu } from './menu'

const items: ItemMenu[] = [
  { etiqueta: 'Postulaciones', ruta: '/estudiante/postulaciones', roles: ['Estudiante'], orden: 20 },
  { etiqueta: 'Vacantes', ruta: '/estudiante/vacantes', roles: ['Estudiante'], orden: 10 },
  { etiqueta: 'Mi empresa', ruta: '/empresa/vacantes', roles: ['AdminEmpresa', 'Reclutador'] },
  { etiqueta: 'Empresas', ruta: '/coordinacion/empresas', roles: ['Coordinador'] },
  { etiqueta: 'Reclutadores', ruta: '/empresa/usuarios', roles: ['AdminEmpresa'] },
]

describe('menuParaRol', () => {
  it('solo devuelve las entradas del rol', () => {
    expect(menuParaRol(items, 'Coordinador').map((i) => i.etiqueta)).toEqual(['Empresas'])
  })

  it('ordena por "orden" de menor a mayor', () => {
    expect(menuParaRol(items, 'Estudiante').map((i) => i.etiqueta)).toEqual(['Vacantes', 'Postulaciones'])
  })

  it('con el mismo orden respeta el orden de declaración', () => {
    expect(menuParaRol(items, 'AdminEmpresa').map((i) => i.etiqueta)).toEqual(['Mi empresa', 'Reclutadores'])
  })

  it('un rol sin entradas recibe una lista vacía', () => {
    expect(menuParaRol(items, 'SuperAdmin')).toEqual([])
  })

  it('no modifica la lista original', () => {
    const copia = [...items]
    menuParaRol(items, 'Estudiante')
    expect(items).toEqual(copia)
  })
})
