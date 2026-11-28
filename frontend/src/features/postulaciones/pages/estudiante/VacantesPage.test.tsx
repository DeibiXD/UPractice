// @vitest-environment jsdom
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { errorHttp } from '@/test/errores'
import { vacanteResumen } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../../api'
import { VacantesPage } from './VacantesPage'

vi.mock('../../api', () => ({ postulacionesApi: { listarVacantes: vi.fn() } }))

afterEach(cleanup)
beforeEach(() => vi.clearAllMocks())

const pagina = (items = [vacanteResumen()], totalPaginas = 1) => ({
  items,
  pagina: 1,
  tamanoPagina: 10,
  totalRegistros: items.length,
  totalPaginas,
})

function montar() {
  return renderEnRuta(<VacantesPage />, {
    patron: '/estudiante/vacantes',
    ubicacion: '/estudiante/vacantes',
    destinos: [{ path: '/estudiante/vacantes/:id', texto: 'DETALLE DE VACANTE' }],
  })
}

describe('VacantesPage', () => {
  it('lista las vacantes con sus datos y el estado del perfil', async () => {
    vi.mocked(api.listarVacantes).mockResolvedValue(
      pagina([
        vacanteResumen(),
        vacanteResumen({ id: 'v2', titulo: 'Analista de Datos', cumploRequisitos: false, yaPostulado: true, modalidad: 'Remota', remunerada: false }),
      ]),
    )
    montar()

    const fila1 = (await screen.findByText('Desarrollador Web')).closest('tr') as HTMLElement
    expect(within(fila1).getByText('Híbrida')).toBeTruthy()
    expect(within(fila1).getByText('Cumple requisitos')).toBeTruthy()
    const fila2 = screen.getByText('Analista de Datos').closest('tr') as HTMLElement
    expect(within(fila2).getByText('Postulado')).toBeTruthy()
    expect(within(fila2).getByText('Requisitos parciales')).toBeTruthy()
  })

  it('al cargar pide la primera página sin filtros', async () => {
    vi.mocked(api.listarVacantes).mockResolvedValue(pagina())
    montar()
    await screen.findByText('Desarrollador Web')
    expect(api.listarVacantes).toHaveBeenCalledWith({ pagina: 1, tamanoPagina: 10 })
  })

  it('buscar aplica los filtros recortados y vuelve a la página 1', async () => {
    vi.mocked(api.listarVacantes).mockResolvedValue(pagina())
    montar()
    await screen.findByText('Desarrollador Web')

    await userEvent.type(screen.getByLabelText('Buscar por título o empresa'), '  web  ')
    await userEvent.selectOptions(screen.getByLabelText('Modalidad'), 'Remota')
    await userEvent.selectOptions(screen.getByLabelText('Remuneración'), 'si')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }))

    await waitFor(() =>
      expect(api.listarVacantes).toHaveBeenLastCalledWith({
        busqueda: 'web',
        modalidad: 'Remota',
        remunerada: true,
        pagina: 1,
        tamanoPagina: 10,
      }),
    )
  })

  it('"No remuneradas" se envía como remunerada=false', async () => {
    vi.mocked(api.listarVacantes).mockResolvedValue(pagina())
    montar()
    await screen.findByText('Desarrollador Web')

    await userEvent.selectOptions(screen.getByLabelText('Remuneración'), 'no')
    await userEvent.click(screen.getByRole('button', { name: 'Buscar' }))

    await waitFor(() => expect(api.listarVacantes).toHaveBeenLastCalledWith(expect.objectContaining({ remunerada: false })))
  })

  it('sin resultados muestra el aviso', async () => {
    vi.mocked(api.listarVacantes).mockResolvedValue(pagina([]))
    montar()
    expect(await screen.findByText('No hay vacantes disponibles con esos filtros.')).toBeTruthy()
  })

  it('con varias páginas permite cambiar de página', async () => {
    vi.mocked(api.listarVacantes).mockResolvedValue(pagina([vacanteResumen()], 3))
    montar()
    await screen.findByText('Desarrollador Web')

    await userEvent.click(screen.getByRole('button', { name: /page 2/i }))
    await waitFor(() => expect(api.listarVacantes).toHaveBeenLastCalledWith(expect.objectContaining({ pagina: 2 })))
  })

  it('al hacer clic en una vacante abre su detalle', async () => {
    vi.mocked(api.listarVacantes).mockResolvedValue(pagina())
    montar()
    await userEvent.click(await screen.findByText('Desarrollador Web'))
    expect(await screen.findByText('DETALLE DE VACANTE')).toBeTruthy()
  })

  it('si falla la carga muestra el mensaje del backend y permite reintentar', async () => {
    vi.mocked(api.listarVacantes).mockRejectedValue(errorHttp(500, { detail: 'Ocurrió un error en el servidor.' }))
    montar()
    expect(await screen.findByText(/Ocurrió un error en el servidor/)).toBeTruthy()
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))
    await waitFor(() => expect(api.listarVacantes).toHaveBeenCalledTimes(2))
  })
})
