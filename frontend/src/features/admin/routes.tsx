import type { RouteObject } from 'react-router-dom'
import { RutaPorRol } from '@/shared/auth/RutaPorRol'
import { CoordinadoresPage } from './pages/CoordinadoresPage'
import { EmpresasPage } from './pages/EmpresasPage'

/** Dentro de Layout y de RutaProtegida. HU-A01 agrega aquí /admin/universidades. */
export const rutasPrivadas: RouteObject[] = [
  {
    element: <RutaPorRol roles={['SuperAdmin']} />,
    children: [
      { path: '/admin/coordinadores', element: <CoordinadoresPage /> },
      { path: '/admin/empresas', element: <EmpresasPage /> },
    ],
  },
]
