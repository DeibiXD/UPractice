// @vitest-environment jsdom
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { errorHttp } from '@/test/errores'
import { CV_PRINCIPAL, CV_SECUNDARIO, miPostulacion } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../../api'
import { MiPostulacionDetallePage } from './MiPostulacionDetallePage'

vi.mock('../../api', () => ({
  postulacionesApi: {
    obtenerMia: vi.fn(),
    retirar: vi.fn(),
    reactivar: vi.fn(),
    responderOferta: vi.fn(),
    listarMisCurriculums: vi.fn(),
  },
}))

afterEach(cleanup)
beforeEach(() => vi.clearAllMocks())

const montar = () =>
  renderEnRuta(<MiPostulacionDetallePage />, {
    patron: '/estudiante/postulaciones/:id',
    ubicacion: '/estudiante/postulaciones/p1',
  })

const confirmar = (nombre: string) => within(screen.getByRole('dialog')).getByRole('button', { name: nombre })

describe('MiPostulacionDetallePage', () => {
  it('muestra la postulación, el currículum enviado y el historial', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion())
    montar()

    expect(await screen.findByRole('heading', { name: 'Desarrollador Web' })).toBeTruthy()
    expect(screen.getByText('Digitales S.A.')).toBeTruthy()
    expect(screen.getByText('CV 2026')).toBeTruthy()
    expect(within(screen.getByRole('list', { name: 'Historial' })).getAllByRole('listitem')).toHaveLength(2)
    expect(api.obtenerMia).toHaveBeenCalledWith('p1')
  })

  it('solo ofrece las acciones que el backend permite (acciones)', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion({ acciones: ['Retirar'] }))
    montar()

    expect(await screen.findByRole('button', { name: 'Retirar postulación' })).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Aceptar oferta' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Reactivar postulación' })).toBeNull()
  })

  it('una postulación sin acciones no muestra botones de acción', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion({ estado: 'Rechazada', acciones: [] }))
    montar()

    await screen.findByRole('heading', { name: 'Desarrollador Web' })
    expect(screen.queryByRole('button', { name: /postulación|oferta/i })).toBeNull()
  })

  it('retirar pide confirmación y llama al backend', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion())
    vi.mocked(api.retirar).mockResolvedValue(miPostulacion({ estado: 'Retirada', acciones: ['Reactivar'] }))
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Retirar postulación' }))
    expect(await screen.findByText(/¿Retirar su postulación a «Desarrollador Web»\?/)).toBeTruthy()
    expect(api.retirar).not.toHaveBeenCalled()

    await userEvent.click(confirmar('Retirar'))
    await waitFor(() => expect(api.retirar).toHaveBeenCalledWith('p1'))
    expect(await screen.findByText('Postulación retirada.')).toBeTruthy()
  })

  it('cancelar la confirmación no llama al backend', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion())
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Retirar postulación' }))
    await userEvent.click(confirmar('Cancelar'))

    expect(api.retirar).not.toHaveBeenCalled()
  })

  it('con una oferta se puede aceptar (con aviso de consecuencias) o declinar', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion({ estado: 'Oferta', acciones: ['AceptarOferta', 'DeclinarOferta'] }))
    vi.mocked(api.responderOferta).mockResolvedValue(miPostulacion({ estado: 'Aceptada', acciones: [] }))
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Aceptar oferta' }))
    expect(await screen.findByText(/demás postulaciones activas se retirarán/)).toBeTruthy()
    await userEvent.click(confirmar('Aceptar oferta'))

    await waitFor(() => expect(api.responderOferta).toHaveBeenCalledWith('p1', { aceptar: true }))
    expect(await screen.findByText('Oferta aceptada.')).toBeTruthy()
  })

  it('declinar la oferta envía aceptar=false', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion({ estado: 'Oferta', acciones: ['AceptarOferta', 'DeclinarOferta'] }))
    vi.mocked(api.responderOferta).mockResolvedValue(miPostulacion({ estado: 'Declinada', acciones: [] }))
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Declinar oferta' }))
    await userEvent.click(confirmar('Declinar oferta'))

    await waitFor(() => expect(api.responderOferta).toHaveBeenCalledWith('p1', { aceptar: false }))
  })

  it('si el backend rechaza la acción muestra su mensaje', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion())
    vi.mocked(api.retirar).mockRejectedValue(errorHttp(422, { detail: 'La postulación ya no se puede retirar.', code: 'TRANSICION_INVALIDA' }))
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Retirar postulación' }))
    await userEvent.click(confirmar('Retirar'))

    expect(await screen.findByText('La postulación ya no se puede retirar.')).toBeTruthy()
  })

  it('reactivar permite elegir un currículum y lo envía', async () => {
    vi.mocked(api.obtenerMia).mockResolvedValue(miPostulacion({ estado: 'Retirada', acciones: ['Reactivar'] }))
    vi.mocked(api.listarMisCurriculums).mockResolvedValue([CV_PRINCIPAL, CV_SECUNDARIO])
    vi.mocked(api.reactivar).mockResolvedValue(miPostulacion())
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Reactivar postulación' }))
    await userEvent.selectOptions(await screen.findByLabelText('Currículum'), 'cv-2')
    await userEvent.click(confirmar('Reactivar'))

    await waitFor(() => expect(api.reactivar).toHaveBeenCalledWith('p1', { curriculumId: 'cv-2' }))
    expect(await screen.findByText('Postulación reactivada.')).toBeTruthy()
  })

  it('una postulación inexistente muestra el error del backend', async () => {
    vi.mocked(api.obtenerMia).mockRejectedValue(errorHttp(404, { detail: 'Postulación no encontrada.' }))
    montar()
    expect(await screen.findByText('Postulación no encontrada.')).toBeTruthy()
  })
})
