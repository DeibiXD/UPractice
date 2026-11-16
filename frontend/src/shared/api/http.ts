import axios from 'axios'
import { sesion } from '@/shared/auth/sesion'

export const API_URL: string = import.meta.env.VITE_API_URL || 'http://localhost:5294/api/v1'

// 60 s: la API gratuita de Render tarda en despertar después de un rato sin uso.
export const http = axios.create({ baseURL: API_URL, timeout: 60_000 })

http.interceptors.request.use((config) => {
  const token = sesion.obtenerToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

http.interceptors.response.use(
  (respuesta) => respuesta,
  (error: unknown) => {
    if (axios.isAxiosError(error)) {
      const status = error.response?.status
      const ruta = window.location.pathname
      const esLogin = (error.config?.url ?? '').includes('/auth/login')

      // Sesión vencida o token inválido (el 401 del login es "credenciales incorrectas", no se redirige).
      if (status === 401 && !esLogin) {
        sesion.cerrar()
        if (ruta !== '/login') window.location.assign('/login')
      }

      // D-17: contraseña temporal. Solo /auth/me y PUT /auth/password responden hasta que se cambie.
      const code = (error.response?.data as { code?: string } | undefined)?.code
      if (status === 403 && code === 'CAMBIO_PASSWORD_REQUERIDO' && ruta !== '/cambiar-password') {
        window.location.assign('/cambiar-password')
      }
    }
    return Promise.reject(error)
  },
)
