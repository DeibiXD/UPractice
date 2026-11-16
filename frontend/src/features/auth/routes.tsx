import type { RouteObject } from 'react-router-dom'
import { CambiarPasswordPage } from './pages/CambiarPasswordPage'
import { LoginPage } from './pages/LoginPage'

/** Sin sesión. Los registros (HU-01 y HU-E01) agregan aquí sus rutas /registro/*. */
export const rutasPublicas: RouteObject[] = [{ path: '/login', element: <LoginPage /> }]

/** Con sesión, sin barra superior. */
export const rutasAutenticadas: RouteObject[] = [{ path: '/cambiar-password', element: <CambiarPasswordPage /> }]
