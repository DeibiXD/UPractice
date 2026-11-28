import { describe, expect, it } from 'vitest'
import { esquemaSubirCurriculum, MENSAJE_ARCHIVO, MENSAJE_NOMBRE_VERSION, TAMANO_MAXIMO } from './curriculum'

const pdf = (nombre = 'cv.pdf', tipo = 'application/pdf', tamano = 10) => new File([new Uint8Array(tamano)], nombre, { type: tipo })

function errores(datos: unknown): Record<string, string> {
  const r = esquemaSubirCurriculum.safeParse(datos)
  if (r.success) return {}
  return Object.fromEntries(r.error.issues.map((i) => [String(i.path[0]), i.message]))
}

describe('esquemaSubirCurriculum (simétrico con SubirCurriculumValidator)', () => {
  it('acepta un PDF de hasta 5 MB y recorta el nombre', () => {
    const r = esquemaSubirCurriculum.parse({ nombreVersion: '  CV web  ', archivo: pdf('CV.PDF', 'application/pdf', TAMANO_MAXIMO) })
    expect(r.nombreVersion).toBe('CV web')
  })

  it.each(['ab', '  ab  ', 'x'.repeat(61)])('rechaza el nombre "%s"', (nombreVersion) => {
    expect(errores({ nombreVersion, archivo: pdf() }).nombreVersion).toBe(MENSAJE_NOMBRE_VERSION)
  })

  it.each([
    ['sin archivo', undefined],
    ['extensión', pdf('cv.docx')],
    ['tipo MIME', pdf('cv.pdf', 'text/plain')],
    ['más de 5 MB', pdf('cv.pdf', 'application/pdf', TAMANO_MAXIMO + 1)],
    ['vacío', pdf('cv.pdf', 'application/pdf', 0)],
  ])('rechaza el archivo (%s)', (_, archivo) => {
    expect(errores({ nombreVersion: 'CV general', archivo }).archivo).toBe(MENSAJE_ARCHIVO)
  })
})
