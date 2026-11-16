import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import type { CambiarPasswordRequest, LoginRequest, UsuarioActualDto } from '@/shared/api/types'
import { authApi } from '@/shared/auth/authApi'
import { AuthContext, type AuthContextValue } from '@/shared/auth/authContexto'
import { sesion } from '@/shared/auth/sesion'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioActualDto | null>(null)
  // Si hay token guardado, hay que consultar /auth/me antes de decidir si hay sesión.
  const [cargando, setCargando] = useState<boolean>(() => sesion.obtenerToken() !== null)

  useEffect(() => {
    if (!sesion.obtenerToken()) return
    let activo = true
    authApi
      .me()
      .then((datos) => {
        if (activo) setUsuario(datos)
      })
      .catch(() => {
        // 401: el interceptor ya cerró la sesión y redirige a /login.
        // Otro error (p. ej. API dormida): se queda sin sesión y verá el login.
      })
      .finally(() => {
        if (activo) setCargando(false)
      })
    return () => {
      activo = false
    }
  }, [])

  const login = useCallback(async (datos: LoginRequest) => {
    const respuesta = await authApi.login(datos)
    sesion.guardarToken(respuesta.accessToken)
    setUsuario(respuesta.usuario)
    return respuesta.usuario
  }, [])

  const cambiarPassword = useCallback(async (datos: CambiarPasswordRequest) => {
    const respuesta = await authApi.cambiarPassword(datos)
    sesion.guardarToken(respuesta.accessToken)
    setUsuario(respuesta.usuario)
    return respuesta.usuario
  }, [])

  const logout = useCallback(() => {
    sesion.cerrar()
    setUsuario(null)
  }, [])

  const valor = useMemo<AuthContextValue>(
    () => ({ usuario, cargando, login, cambiarPassword, logout }),
    [usuario, cargando, login, cambiarPassword, logout],
  )

  return <AuthContext.Provider value={valor}>{children}</AuthContext.Provider>
}
