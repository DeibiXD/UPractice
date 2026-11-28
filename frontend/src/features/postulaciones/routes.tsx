import type { RouteObject } from 'react-router-dom'
import { RutaPorRol } from '@/shared/auth/RutaPorRol'
import { MiPostulacionDetallePage } from './pages/estudiante/MiPostulacionDetallePage'
import { MisPostulacionesPage } from './pages/estudiante/MisPostulacionesPage'
import { VacanteDetallePage } from './pages/estudiante/VacanteDetallePage'
import { VacantesPage } from './pages/estudiante/VacantesPage'
import { PostulanteDetallePage } from './pages/empresa/PostulanteDetallePage'
import { PostulantesPage } from './pages/empresa/PostulantesPage'
import { PostulantesVacantePage } from './pages/empresa/PostulantesVacantePage'

/** Dentro de Layout y de RutaProtegida (las compone src/app/router.tsx). */
export const rutasPrivadas: RouteObject[] = [
  {
    element: <RutaPorRol roles={['Estudiante']} />,
    children: [
      { path: '/estudiante/vacantes', element: <VacantesPage /> },
      { path: '/estudiante/vacantes/:id', element: <VacanteDetallePage /> },
      { path: '/estudiante/postulaciones', element: <MisPostulacionesPage /> },
      { path: '/estudiante/postulaciones/:id', element: <MiPostulacionDetallePage /> },
    ],
  },
  {
    element: <RutaPorRol roles={['AdminEmpresa', 'Reclutador']} />,
    children: [
      { path: '/empresa/postulantes', element: <PostulantesPage /> },
      { path: '/empresa/vacantes/:vacanteId/postulantes', element: <PostulantesVacantePage /> },
      { path: '/empresa/postulaciones/:id', element: <PostulanteDetallePage /> },
    ],
  },
]
