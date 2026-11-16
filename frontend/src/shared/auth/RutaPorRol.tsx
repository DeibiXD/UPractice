import { Outlet } from 'react-router-dom'
import type { Rol } from '@/shared/api/types'
import { useAuth } from '@/shared/auth/useAuth'
import { PaginaSinAcceso } from '@/shared/components/PaginaSinAcceso'

/** Va DENTRO de RutaProtegida: { element: <RutaPorRol roles={['Coordinador']} />, children: [...] }. */
export function RutaPorRol({ roles }: { roles: Rol[] }) {
  const { usuario } = useAuth()
  if (!usuario || !roles.includes(usuario.rol)) return <PaginaSinAcceso />
  return <Outlet />
}
