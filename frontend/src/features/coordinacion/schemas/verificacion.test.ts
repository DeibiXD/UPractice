import { describe, expect, it } from 'vitest'
import { esquemaRechazoVerificacion, MENSAJE_OBSERVACION } from './verificacion'

describe('esquemaRechazoVerificacion (simétrico con ResolverVerificacionValidator al rechazar)', () => {
  it('acepta 10 a 500 caracteres y recorta espacios', () => {
    expect(esquemaRechazoVerificacion.parse({ observacion: '  Índice no coincide.  ' }).observacion).toBe('Índice no coincide.')
  })

  it.each(['', 'corta', '   nueve   ', 'x'.repeat(501)])('rechaza "%s"', (observacion) => {
    const r = esquemaRechazoVerificacion.safeParse({ observacion })
    expect(r.success).toBe(false)
    expect(r.error?.issues[0].message).toBe(MENSAJE_OBSERVACION)
  })
})
