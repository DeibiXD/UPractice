import { renderToStaticMarkup } from 'react-dom/server'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { rutasPrivadas } from '@/features/inicio'
import type { Rol, UsuarioActualDto } from '@/shared/api/types'
import { AuthContext, type AuthContextValue } from '@/shared/auth/authContexto'
import { Layout } from '@/shared/components/Layout'
import type { ItemMenu } from '@/shared/navegacion/menu'

const menu: ItemMenu[] = [
  { etiqueta: 'Vacantes', ruta: '/estudiante/vacantes', roles: ['Estudiante'], descripcion: 'Explore las vacantes.', orden: 10 },
  { etiqueta: 'Mis postulaciones', ruta: '/estudiante/postulaciones', roles: ['Estudiante'], orden: 20 },
  { etiqueta: 'Panel de vacantes', ruta: '/empresa/vacantes', roles: ['AdminEmpresa', 'Reclutador'] },
  { etiqueta: 'Empresas', ruta: '/coordinacion/empresas', roles: ['Coordinador'] },
]

function usuario(rol: Rol): UsuarioActualDto {
  return {
    id: '1',
    nombreCompleto: 'Persona Prueba',
    correo: 'prueba@upractice.test',
    rol,
    empresa: rol === 'AdminEmpresa' ? { id: 'e1', nombre: 'Acme S.A.' } : null,
    universidad: null,
    debeCambiarPassword: false,
  }
}

function pintar(rol: Rol, entradas: ItemMenu[] = menu): string {
  const valor: AuthContextValue = {
    usuario: usuario(rol),
    cargando: false,
    login: async () => usuario(rol),
    cambiarPassword: async () => usuario(rol),
    logout: () => {},
  }
  return renderToStaticMarkup(
    <MemoryRouter initialEntries={['/']}>
      <AuthContext.Provider value={valor}>
        <Routes>
          <Route element={<Layout menu={entradas} />}>
            <Route path="/" element={rutasPrivadas[0].element} />
          </Route>
        </Routes>
      </AuthContext.Provider>
    </MemoryRouter>,
  )
}

describe('Layout e Inicio por rol', () => {
  it('el estudiante ve sus secciones y no las de otros roles', () => {
    const html = pintar('Estudiante')
    expect(html).toContain('Vacantes')
    expect(html).toContain('Mis postulaciones')
    expect(html).not.toContain('Panel de vacantes')
    expect(html).not.toContain('Empresas')
  })

  it('el coordinador ve solo la suya', () => {
    const html = pintar('Coordinador')
    expect(html).toContain('Empresas')
    expect(html).not.toContain('Mis postulaciones')
  })

  it('"Inicio" siempre está primero y el menú respeta el orden', () => {
    const html = pintar('Estudiante')
    expect(html.indexOf('>Inicio<')).toBeGreaterThan(-1)
    expect(html.indexOf('>Inicio<')).toBeLessThan(html.indexOf('>Vacantes<'))
    expect(html.indexOf('>Vacantes<')).toBeLessThan(html.indexOf('>Mis postulaciones<'))
  })

  it('la pantalla de inicio saluda y muestra la tarjeta con la descripción', () => {
    const html = pintar('Estudiante')
    expect(html).toContain('Hola, Persona Prueba')
    expect(html).toContain('Explore las vacantes.')
  })

  it('muestra el rol y la organización', () => {
    const html = pintar('AdminEmpresa')
    expect(html).toContain('Administrador de empresa · Acme S.A.')
  })

  it('un rol sin secciones ve el aviso de vacío', () => {
    expect(pintar('SuperAdmin')).toContain('Aún no hay secciones disponibles')
  })

  it('incluye el botón para cerrar sesión y el de menú móvil', () => {
    const html = pintar('Estudiante')
    expect(html).toContain('Salir')
    expect(html).toContain('Abrir menú')
  })
})
