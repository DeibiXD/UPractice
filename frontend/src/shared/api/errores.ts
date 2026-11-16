import axios from 'axios'
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'

/** Forma única que reciben las pantallas, sin importar qué falló. */
export interface ErrorApi {
  /** Para un Alert o Snackbar. */
  mensaje: string
  /** Errores por campo (400), con la misma llave camelCase que el formulario. */
  campos: Record<string, string>
  /** Regla de negocio o conflicto (409/422/403): p. ej. PERFIL_NO_VERIFICADO. */
  code?: string
  status?: number
  traceId?: string
}

// ProblemDetails (RFC 9457) como lo arma backend/.../Errores/Problemas.cs (S5.3 del diseño).
interface ProblemaApi {
  title?: string
  detail?: string
  code?: string
  traceId?: string
  errors?: Record<string, string[]>
}

const MENSAJE_SIN_CONEXION =
  'No se pudo conectar con el servidor. Verifique su conexión; si la API estaba inactiva, espere unos segundos e intente de nuevo.'
const MENSAJE_TIEMPO_AGOTADO =
  'El servidor tardó demasiado en responder. Intente de nuevo en unos segundos.'
const MENSAJE_INESPERADO = 'Ocurrió un error inesperado. Intente de nuevo.'

function aplanarCampos(errores: ProblemaApi['errors']): Record<string, string> {
  const campos: Record<string, string> = {}
  for (const [campo, mensajes] of Object.entries(errores ?? {})) {
    // Una llave vacía es un error de todo el objeto, no de un campo.
    if (campo !== '' && mensajes.length > 0) campos[campo] = mensajes[0]
  }
  return campos
}

/** Extractor universal: nunca lanza y siempre devuelve un ErrorApi. */
export function extraerError(error: unknown): ErrorApi {
  if (!axios.isAxiosError(error)) {
    return { mensaje: MENSAJE_INESPERADO, campos: {} }
  }

  const respuesta = error.response
  if (!respuesta) {
    return {
      mensaje: error.code === 'ECONNABORTED' ? MENSAJE_TIEMPO_AGOTADO : MENSAJE_SIN_CONEXION,
      campos: {},
    }
  }

  const status = respuesta.status
  const problema: ProblemaApi =
    typeof respuesta.data === 'object' && respuesta.data !== null ? (respuesta.data as ProblemaApi) : {}
  const campos = aplanarCampos(problema.errors)
  const base = { status, code: problema.code, traceId: problema.traceId, campos }

  if (status === 400) {
    return { ...base, mensaje: problema.title ?? problema.detail ?? 'Revise los campos marcados.' }
  }
  if (status === 401) {
    return { ...base, mensaje: problema.detail ?? 'Su sesión expiró o las credenciales no son válidas.' }
  }
  if (status === 403) {
    return { ...base, mensaje: problema.detail ?? 'No tiene permiso para esta operación.' }
  }
  if (status === 404) {
    return { ...base, mensaje: problema.detail ?? 'No se encontró el recurso.' }
  }
  if (status === 409 || status === 422) {
    return { ...base, mensaje: problema.detail ?? problema.title ?? 'No es posible completar la operación.' }
  }
  if (status >= 500) {
    const detalle = problema.detail ?? 'Ocurrió un error en el servidor. Intente de nuevo.'
    return { ...base, mensaje: problema.traceId ? `${detalle} (código: ${problema.traceId})` : detalle }
  }
  return { ...base, mensaje: problema.detail ?? problema.title ?? MENSAJE_INESPERADO }
}

/** Pinta en cada campo del formulario el error que devolvió el backend (400). */
export function aplicarErrores<T extends FieldValues>(error: ErrorApi, setError: UseFormSetError<T>): void {
  for (const [campo, mensaje] of Object.entries(error.campos)) {
    setError(campo as Path<T>, { type: 'server', message: mensaje })
  }
}
