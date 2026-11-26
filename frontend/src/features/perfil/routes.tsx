import type { RouteObject } from 'react-router-dom'
import { RutaPorRol } from '@/shared/auth/RutaPorRol'
import { PerfilPage } from './pages/PerfilPage'

/** Dentro de Layout y de RutaProtegida. */
export const rutasPrivadas: RouteObject[] = [
  { element: <RutaPorRol roles={['Estudiante']} />, children: [{ path: '/estudiante/perfil', element: <PerfilPage /> }] },
]
