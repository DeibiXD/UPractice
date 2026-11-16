import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/shared/auth/useAuth'
import { Cargando } from '@/shared/components/Cargando'

/** Exige sesión. Con contraseña temporal (D-17) solo deja pasar a /cambiar-password. */
export function RutaProtegida() {
  const { usuario, cargando } = useAuth()
  const ubicacion = useLocation()

  if (cargando) return <Cargando />
  if (!usuario) return <Navigate to="/login" replace state={{ desde: ubicacion.pathname }} />
  if (usuario.debeCambiarPassword && ubicacion.pathname !== '/cambiar-password') {
    return <Navigate to="/cambiar-password" replace />
  }
  return <Outlet />
}
