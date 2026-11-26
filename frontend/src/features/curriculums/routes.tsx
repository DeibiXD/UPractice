import type { RouteObject } from 'react-router-dom'
import { RutaPorRol } from '@/shared/auth/RutaPorRol'
import { CurriculumsPage } from './pages/CurriculumsPage'

/** Dentro de Layout y de RutaProtegida. */
export const rutasPrivadas: RouteObject[] = [
  {
    element: <RutaPorRol roles={['Estudiante']} />,
    children: [{ path: '/estudiante/curriculums', element: <CurriculumsPage /> }],
  },
]
