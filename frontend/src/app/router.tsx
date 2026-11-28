import { createBrowserRouter } from 'react-router-dom'
import {
  rutasAutenticadas as rutasAuthAutenticadas,
  rutasPublicas as rutasAuthPublicas,
} from '@/features/auth'
import { rutasPrivadas as rutasAdmin } from '@/features/admin'
import { rutasPrivadas as rutasInicio } from '@/features/inicio'
import { menu } from '@/app/menu'
import { RutaProtegida } from '@/shared/auth/RutaProtegida'
import { Layout } from '@/shared/components/Layout'
import { PaginaNoEncontrada } from '@/shared/components/PaginaNoEncontrada'

/**
 * El router solo COMPONE: cada feature exporta sus rutas desde su index.ts
 * (con su propio <RutaPorRol> cuando aplique). Para agregar una feature:
 *   1. importe sus rutas arriba, y
 *   2. agréguelas con spread donde dice "FEATURES".
 */
export const router = createBrowserRouter([
  // Públicas (sin sesión)
  ...rutasAuthPublicas,

  {
    // Todo lo de abajo exige sesión (y redirige a /cambiar-password si la contraseña es temporal)
    element: <RutaProtegida />,
    children: [
      // Autenticadas sin barra superior
      ...rutasAuthAutenticadas,

      {
        element: <Layout menu={menu} />,
        children: [
          ...rutasInicio,
          ...rutasAdmin,
          // FEATURES: ...rutasVacantes, ...rutasEmpresa, ...rutasCoordinacion, ...
        ],
      },
    ],
  },

  { path: '*', element: <PaginaNoEncontrada /> },
])
