import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import type { FieldValues, UseFormSetError } from 'react-hook-form'
import { describe, expect, it, vi } from 'vitest'
import { aplicarErrores, extraerError } from './errores'

function conRespuesta(status: number, data: unknown): AxiosError {
  const config = { headers: {} } as InternalAxiosRequestConfig
  const respuesta = { status, data, statusText: '', headers: {}, config } as AxiosResponse
  return new AxiosError('fallo', 'ERR_BAD_REQUEST', config, undefined, respuesta)
}

describe('extraerError', () => {
  it('sin respuesta del servidor avisa que no hay conexión', () => {
    const error = extraerError(new AxiosError('Network Error', 'ERR_NETWORK'))
    expect(error.mensaje).toContain('No se pudo conectar')
    expect(error.status).toBeUndefined()
  })

  it('tiempo agotado se distingue de la falta de conexión', () => {
    const error = extraerError(new AxiosError('timeout', 'ECONNABORTED'))
    expect(error.mensaje).toContain('tardó demasiado')
  })

  it('400 devuelve los campos con la misma llave camelCase y toma el primer mensaje', () => {
    const error = extraerError(
      conRespuesta(400, {
        title: 'Uno o más campos no son válidos.',
        errors: {
          correo: ['Ingrese un correo válido.', 'otro'],
          'admin.password': ['La contraseña debe tener 8 a 64 caracteres, con mayúscula, minúscula y número.'],
          '': ['error general'],
        },
      }),
    )
    expect(error.status).toBe(400)
    expect(error.mensaje).toBe('Uno o más campos no son válidos.')
    expect(error.campos).toEqual({
      correo: 'Ingrese un correo válido.',
      'admin.password': 'La contraseña debe tener 8 a 64 caracteres, con mayúscula, minúscula y número.',
    })
  })

  it('401 usa el detalle del backend', () => {
    const error = extraerError(conRespuesta(401, { detail: 'Correo o contraseña incorrectos.' }))
    expect(error.status).toBe(401)
    expect(error.mensaje).toBe('Correo o contraseña incorrectos.')
  })

  it('403 conserva el code de la regla', () => {
    const error = extraerError(
      conRespuesta(403, {
        detail: 'Debe cambiar su contraseña temporal antes de continuar.',
        code: 'CAMBIO_PASSWORD_REQUERIDO',
      }),
    )
    expect(error.code).toBe('CAMBIO_PASSWORD_REQUERIDO')
  })

  it('409 y 422 devuelven mensaje y code', () => {
    const conflicto = extraerError(conRespuesta(409, { detail: 'El correo ya está en uso.', code: 'CORREO_EN_USO' }))
    const regla = extraerError(conRespuesta(422, { detail: 'Verifique su perfil.', code: 'PERFIL_NO_VERIFICADO' }))
    expect(conflicto).toMatchObject({ status: 409, code: 'CORREO_EN_USO', mensaje: 'El correo ya está en uso.' })
    expect(regla).toMatchObject({ status: 422, code: 'PERFIL_NO_VERIFICADO' })
  })

  it('404 sin detalle usa un mensaje por defecto', () => {
    expect(extraerError(conRespuesta(404, {})).mensaje).toBe('No se encontró el recurso.')
  })

  it('500 incluye el traceId para soporte', () => {
    const error = extraerError(conRespuesta(500, { detail: 'Intente de nuevo.', traceId: 'abc-123' }))
    expect(error.mensaje).toContain('abc-123')
  })

  it('una respuesta que no es JSON (p. ej. HTML de un proxy) no rompe el extractor', () => {
    const error = extraerError(conRespuesta(502, '<html>Bad Gateway</html>'))
    expect(error.status).toBe(502)
    expect(error.mensaje.length).toBeGreaterThan(0)
    expect(error.campos).toEqual({})
  })

  it('un error que no viene de Axios devuelve un mensaje genérico', () => {
    expect(extraerError(new Error('boom')).mensaje).toBe('Ocurrió un error inesperado. Intente de nuevo.')
  })
})

describe('aplicarErrores', () => {
  it('llama a setError una vez por campo con type "server"', () => {
    const setError = vi.fn()
    aplicarErrores(
      { mensaje: 'x', campos: { passwordActual: 'La contraseña actual no es correcta.' } },
      setError as unknown as UseFormSetError<FieldValues>,
    )
    expect(setError).toHaveBeenCalledWith('passwordActual', {
      type: 'server',
      message: 'La contraseña actual no es correcta.',
    })
  })
})
