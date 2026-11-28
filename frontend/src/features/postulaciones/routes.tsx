import type { RouteObject } from 'react-router-dom'
import { RutaPorRol } from '@/shared/auth/RutaPorRol'
import { MiPostulacionDetallePage } from './pages/MiPostulacionDetallePage'
import { MisPostulacionesPage } from './pages/MisPostulacionesPage'

/** Dentro de Layout y de RutaProtegida; solo Estudiante (HU-06). */
export const rutasEstudiante: RouteObject[] = [
  {
    element: <RutaPorRol roles={['Estudiante']} />,
    children: [
      { path: '/estudiante/postulaciones', element: <MisPostulacionesPage /> },
      { path: '/estudiante/postulaciones/:id', element: <MiPostulacionDetallePage /> },
    ],
  },
]
