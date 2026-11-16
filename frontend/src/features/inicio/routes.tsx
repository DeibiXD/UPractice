import type { RouteObject } from 'react-router-dom'
import { InicioPage } from './pages/InicioPage'

/** Dentro de Layout y de RutaProtegida. */
export const rutasPrivadas: RouteObject[] = [{ path: '/', element: <InicioPage /> }]
