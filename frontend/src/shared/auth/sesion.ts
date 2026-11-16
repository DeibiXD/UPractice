const CLAVE_TOKEN = 'upractice.token'

/**
 * El token se guarda en localStorage y NUNCA se decodifica (viaja cifrado).
 * Los datos del usuario vienen de LoginResponse.usuario y de GET /auth/me.
 */
export const sesion = {
  obtenerToken(): string | null {
    try {
      return localStorage.getItem(CLAVE_TOKEN)
    } catch {
      return null
    }
  },
  guardarToken(token: string): void {
    try {
      localStorage.setItem(CLAVE_TOKEN, token)
    } catch {
      // Sin almacenamiento disponible: la sesión dura solo mientras la pestaña esté abierta.
    }
  },
  cerrar(): void {
    try {
      localStorage.removeItem(CLAVE_TOKEN)
    } catch {
      // Nada que limpiar.
    }
  },
}
