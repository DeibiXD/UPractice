import { createContext } from 'react'
import type { CambiarPasswordRequest, LoginRequest, UsuarioActualDto } from '@/shared/api/types'

export interface AuthContextValue {
  /** null mientras no hay sesión. */
  usuario: UsuarioActualDto | null
  /** true mientras se recupera la sesión con GET /auth/me al recargar la página. */
  cargando: boolean
  login: (datos: LoginRequest) => Promise<UsuarioActualDto>
  cambiarPassword: (datos: CambiarPasswordRequest) => Promise<UsuarioActualDto>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
