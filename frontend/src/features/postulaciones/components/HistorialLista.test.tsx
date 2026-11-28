// @vitest-environment jsdom
import { cleanup, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { renderEnRuta } from '@/test/render'
import { HistorialLista } from './HistorialLista'

afterEach(cleanup)

describe('HistorialLista', () => {
  it('muestra lo más reciente primero, con comentario y autor', () => {
    renderEnRuta(
      <HistorialLista
        items={[
          { estadoAnterior: null, estadoNuevo: 'Enviada', fecha: '2026-11-20T15:00:00Z', comentario: null, cambiadoPor: 'Ana' },
          { estadoAnterior: 'Enviada', estadoNuevo: 'EnRevision', fecha: '2026-11-21T16:30:00Z', comentario: 'Lo revisamos.', cambiadoPor: 'Reclutador Demo' },
        ]}
      />,
    )

    const filas = within(screen.getByRole('list', { name: 'Historial' })).getAllByRole('listitem')
    expect(filas).toHaveLength(2)
    expect(within(filas[0]).getByText('En revisión')).toBeTruthy()
    expect(within(filas[0]).getByText('Lo revisamos.')).toBeTruthy()
    expect(filas[0].textContent).toContain('Reclutador Demo')
    expect(within(filas[1]).getByText('Enviada')).toBeTruthy()
  })

  it('sin movimientos muestra un aviso', () => {
    renderEnRuta(<HistorialLista items={[]} />)
    expect(screen.getByText('Sin movimientos todavía.')).toBeTruthy()
  })
})
