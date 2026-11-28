// @vitest-environment jsdom
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { miPostulacion } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../../api'
import { MisPostulacionesPage } from './MisPostulacionesPage'

vi.mock('../../api', () => ({ postulacionesApi: { listarMias: vi.fn() } }))

afterEach(cleanup)
beforeEach(() => vi.clearAllMocks())

const montar = () =>
  renderEnRuta(<MisPostulacionesPage />, {
    patron: '/estudiante/postulaciones',
    ubicacion: '/estudiante/postulaciones',
    destinos: [{ path: '/estudiante/postulaciones/:id', texto: 'DETALLE DE POSTULACION' }],
  })

describe('MisPostulacionesPage', () => {
  it('lista las postulaciones con su estado', async () => {
    vi.mocked(api.listarMias).mockResolvedValue([
      miPostulacion(),
      miPostulacion({ id: 'p2', vacante: { id: 'v2', titulo: 'Analista de Datos' }, estado: 'Oferta' }),
    ])
    montar()

    const fila1 = (await screen.findByText('Desarrollador Web')).closest('tr') as HTMLElement
    expect(within(fila1).getByText('Digitales S.A.')).toBeTruthy()
    expect(within(fila1).getByText('Enviada')).toBeTruthy()
    const fila2 = screen.getByText('Analista de Datos').closest('tr') as HTMLElement
    expect(within(fila2).getByText('Oferta')).toBeTruthy()
  })

  it('filtrar por estado vuelve a pedir la lista con ese estado', async () => {
    vi.mocked(api.listarMias).mockResolvedValue([miPostulacion()])
    montar()
    await screen.findByText('Desarrollador Web')
    expect(api.listarMias).toHaveBeenLastCalledWith({ estado: undefined })

    await userEvent.selectOptions(screen.getByLabelText('Estado'), 'Oferta')
    await waitFor(() => expect(api.listarMias).toHaveBeenLastCalledWith({ estado: 'Oferta' }))
  })

  it('sin postulaciones invita a explorar vacantes', async () => {
    vi.mocked(api.listarMias).mockResolvedValue([])
    montar()

    expect(await screen.findByText('Aún no tiene postulaciones.')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Explorar vacantes' }).getAttribute('href')).toBe('/estudiante/vacantes')
  })

  it('sin resultados con un filtro activo no invita a explorar', async () => {
    vi.mocked(api.listarMias).mockResolvedValueOnce([miPostulacion()]).mockResolvedValue([])
    montar()
    await screen.findByText('Desarrollador Web')

    await userEvent.selectOptions(screen.getByLabelText('Estado'), 'Aceptada')
    expect(await screen.findByText('No tiene postulaciones en ese estado.')).toBeTruthy()
    expect(screen.queryByRole('link', { name: 'Explorar vacantes' })).toBeNull()
  })

  it('al hacer clic en una postulación abre su detalle', async () => {
    vi.mocked(api.listarMias).mockResolvedValue([miPostulacion()])
    montar()
    await userEvent.click(await screen.findByText('Desarrollador Web'))
    expect(await screen.findByText('DETALLE DE POSTULACION')).toBeTruthy()
  })
})
