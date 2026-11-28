import { describe, expect, it } from 'vitest'
import { esquemaPerfilAcademico, MENSAJE_CARRERA, MENSAJE_INDICE, MENSAJE_NUMERO_CUENTA } from './perfil'

const valido = { numeroCuenta: '20221234567', carreraId: 'c1', indiceAcademico: 85.25 }

function errores(datos: unknown): Record<string, string> {
  const r = esquemaPerfilAcademico.safeParse(datos)
  if (r.success) return {}
  return Object.fromEntries(r.error.issues.map((i) => [String(i.path[0]), i.message]))
}

describe('esquemaPerfilAcademico (simétrico con ActualizarPerfilAcademicoValidator)', () => {
  it('acepta un perfil válido y recorta el número de cuenta', () => {
    const r = esquemaPerfilAcademico.parse({ ...valido, numeroCuenta: ' 20221234567 ' })
    expect(r.numeroCuenta).toBe('20221234567')
  })

  it.each(['12345', '123456789012345678901', '2022-12345', 'abc123456'])('rechaza el número de cuenta %s', (numeroCuenta) => {
    expect(errores({ ...valido, numeroCuenta }).numeroCuenta).toBe(MENSAJE_NUMERO_CUENTA)
  })

  it('exige carrera', () => {
    expect(errores({ ...valido, carreraId: '' }).carreraId).toBe(MENSAJE_CARRERA)
  })

  it.each([-1, 100.5, 85.255, Number.NaN])('rechaza el índice %s', (indiceAcademico) => {
    expect(errores({ ...valido, indiceAcademico }).indiceAcademico).toBe(MENSAJE_INDICE)
  })

  it.each([0, 100, 70.5, 99.99])('acepta el índice %s', (indiceAcademico) => {
    expect(errores({ ...valido, indiceAcademico })).toEqual({})
  })
})
