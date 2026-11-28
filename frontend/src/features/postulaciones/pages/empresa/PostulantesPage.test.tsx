// @vitest-environment jsdom
import { cleanup, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { vacanteEmpresa } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../../api'
import { PostulantesPage } from './PostulantesPage'

vi.mock('../../api', () => ({ postulacionesApi: { listarVacantesEmpresa: vi.fn() } }))

afterEach(cleanup)
beforeEach(() => vi.clearAllMocks())

const pagina = (items = [vacanteEmpresa()]) => ({ items, pagina: 1, tamanoPagina: 10, totalRegistros: items.length, totalPaginas: 1 })

describe('PostulantesPage', () => {
  it('lista las vacantes con su conteo de postulaciones y desglose por estado', async () => {
    vi.mocked(api.listarVacantesEmpresa).mockResolvedValue(
      pagina([vacanteEmpresa(), vacanteEmpresa({ id: 'v2', titulo: 'Soporte TI', estado: 'Pausada', totalPostulaciones: 0, postulacionesPorEstado: {} })]),
    )
    renderEnRuta(<PostulantesPage />)

    const fila = (await screen.findByText('Desarrollador Web')).closest('tr') as HTMLElement
    expect(within(fila).getByText('Publicada')).toBeTruthy()
    expect(within(fila).getByText('3')).toBeTruthy()
    expect(within(fila).getByText('Enviada: 2')).toBeTruthy()
    expect(within(fila).getByText('En revisión: 1')).toBeTruthy()
    expect(within(fila).getByRole('link', { name: 'Ver postulantes' }).getAttribute('href')).toBe(
      '/empresa/vacantes/v1/postulantes',
    )

    const fila2 = screen.getByText('Soporte TI').closest('tr') as HTMLElement
    expect(within(fila2).queryByText(/Enviada:/)).toBeNull()
  })

  it('sin vacantes muestra el aviso', async () => {
    vi.mocked(api.listarVacantesEmpresa).mockResolvedValue(pagina([]))
    renderEnRuta(<PostulantesPage />)
    expect(await screen.findByText('Aún no hay vacantes.')).toBeTruthy()
  })
})
