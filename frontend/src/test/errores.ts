import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'

/** Error de Axios con la respuesta que daría el backend (ProblemDetails). */
export function errorHttp(status: number, data: unknown): AxiosError {
  const config = { headers: {} } as InternalAxiosRequestConfig
  const respuesta = { status, data, statusText: '', headers: {}, config } as AxiosResponse
  return new AxiosError('fallo', 'ERR_BAD_REQUEST', config, undefined, respuesta)
}
