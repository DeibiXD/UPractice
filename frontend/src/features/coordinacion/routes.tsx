import type { RouteObject } from 'react-router-dom'
import { RutaPorRol } from '@/shared/auth/RutaPorRol'
import { VerificacionEstudiantesPage } from './pages/VerificacionEstudiantesPage'

/** Dentro de Layout y de RutaProtegida. HU-C01 agrega aquí /coordinacion/empresas. */
export const rutasPrivadas: RouteObject[] = [
  {
    element: <RutaPorRol roles={['Coordinador']} />,
    children: [{ path: '/coordinacion/estudiantes', element: <VerificacionEstudiantesPage /> }],
  },
]
