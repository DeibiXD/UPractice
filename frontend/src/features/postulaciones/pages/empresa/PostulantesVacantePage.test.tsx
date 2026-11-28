// @vitest-environment jsdom
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { postulanteResumen, vacanteEmpresa } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../../api'
import { PostulantesVacantePage } from './PostulantesVacantePage'

vi.mock('../../api', () => ({
  postulacionesApi: { listarPostulantes: vi.fn(), obtenerVacanteEmpresa: vi.fn() },
}))

afterEach(cleanup)
beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(api.obtenerVacanteEmpresa).mockResolvedValue(vacanteEmpresa())
})

const montar = () =>
  renderEnRuta(<PostulantesVacantePage />, {
    patron: '/empresa/vacantes/:vacanteId/postulantes',
    ubicacion: '/empresa/vacantes/v1/postulantes',
    destinos: [{ path: '/empresa/postulaciones/:id', texto: 'DETALLE DEL POSTULANTE' }],
  })

describe('PostulantesVacantePage', () => {
  it('lista a los postulantes con su estado y cumplimiento de requisitos', async () => {
    vi.mocked(api.listarPostulantes).mockResolvedValue([
      postulanteResumen(),
      postulanteResumen({ postulacionId: 'p2', estudiante: { nombreCompleto: 'Diego Ramos', universidad: 'UNAH', carrera: 'Ingeniería Industrial' }, estado: 'Entrevista', cumpleRequisitos: false }),
    ])
    montar()

    const fila1 = (await screen.findByText('Ana López')).closest('tr') as HTMLElement
    expect(within(fila1).getByText('Cumple')).toBeTruthy()
    const fila2 = screen.getByText('Diego Ramos').closest('tr') as HTMLElement
    expect(within(fila2).getByText('Parcial')).toBeTruthy()
    expect(within(fila2).getByText('Entrevista')).toBeTruthy()
    expect(api.listarPostulantes).toHaveBeenCalledWith('v1', { estado: undefined, cumpleRequisitos: undefined })
  })

  it('muestra el título y el total de la vacante', async () => {
    vi.mocked(api.listarPostulantes).mockResolvedValue([postulanteResumen()])
    montar()
    expect(await screen.findByText('Postulantes de «Desarrollador Web»')).toBeTruthy()
    expect(screen.getByText('3 postulación(es) en total.')).toBeTruthy()
  })

  it('filtra por estado y por requisitos', async () => {
    vi.mocked(api.listarPostulantes).mockResolvedValue([postulanteResumen()])
    montar()
    await screen.findByText('Ana López')

    await userEvent.selectOptions(screen.getByLabelText('Estado'), 'EnRevision')
    await waitFor(() => expect(api.listarPostulantes).toHaveBeenLastCalledWith('v1', { estado: 'EnRevision', cumpleRequisitos: undefined }))

    await userEvent.selectOptions(screen.getByLabelText('Requisitos'), 'si')
    await waitFor(() => expect(api.listarPostulantes).toHaveBeenLastCalledWith('v1', { estado: 'EnRevision', cumpleRequisitos: true }))

    await userEvent.selectOptions(screen.getByLabelText('Requisitos'), 'no')
    await waitFor(() => expect(api.listarPostulantes).toHaveBeenLastCalledWith('v1', { estado: 'EnRevision', cumpleRequisitos: false }))
  })

  it('distingue "sin postulantes" de "ningún postulante coincide"', async () => {
    vi.mocked(api.listarPostulantes).mockResolvedValue([])
    montar()
    expect(await screen.findByText('Aún no hay postulantes en esta vacante.')).toBeTruthy()

    await userEvent.selectOptions(screen.getByLabelText('Estado'), 'Oferta')
    expect(await screen.findByText('Ningún postulante coincide con los filtros.')).toBeTruthy()
  })

  it('al hacer clic en un postulante abre su detalle', async () => {
    vi.mocked(api.listarPostulantes).mockResolvedValue([postulanteResumen()])
    montar()
    await userEvent.click(await screen.findByText('Ana López'))
    expect(await screen.findByText('DETALLE DEL POSTULANTE')).toBeTruthy()
  })
})
