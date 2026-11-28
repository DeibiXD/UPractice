import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { Paginacion } from './Paginacion'

describe('Paginacion', () => {
  it('con una sola página no pinta nada', () => {
    expect(renderToStaticMarkup(<Paginacion pagina={1} totalPaginas={1} onCambiar={() => {}} />)).toBe('')
  })

  it('con varias páginas pinta el paginador con la actual marcada', () => {
    const html = renderToStaticMarkup(<Paginacion pagina={2} totalPaginas={4} onCambiar={() => {}} />)
    expect(html).toContain('<nav')
    expect(html).toMatch(/aria-current="page"[^>]*>2</)
  })
})
