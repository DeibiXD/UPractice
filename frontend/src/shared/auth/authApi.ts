import { http } from '@/shared/api/http'
import type {
  CambiarPasswordRequest,
  LoginRequest,
  LoginResponse,
  UsuarioActualDto,
} from '@/shared/api/types'

export const authApi = {
  login: async (datos: LoginRequest): Promise<LoginResponse> =>
    (await http.post<LoginResponse>('/auth/login', datos)).data,

  me: async (): Promise<UsuarioActualDto> => (await http.get<UsuarioActualDto>('/auth/me')).data,

  /** Devuelve un token nuevo (sin la marca de contraseña temporal). */
  cambiarPassword: async (datos: CambiarPasswordRequest): Promise<LoginResponse> =>
    (await http.put<LoginResponse>('/auth/password', datos)).data,
}
