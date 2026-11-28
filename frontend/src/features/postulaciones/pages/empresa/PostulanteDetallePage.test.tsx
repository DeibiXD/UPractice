// @vitest-environment jsdom
import { cleanup, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { errorHttp } from '@/test/errores'
import { postulante } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../../api'
import { PostulanteDetallePage } from './PostulanteDetallePage'

vi.mock('../../api', () => ({
  postulacionesApi: { obtenerPostulante: vi.fn(), cambiarEstado: vi.fn(), urlCurriculum: vi.fn() },
}))

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})
beforeEach(() => vi.clearAllMocks())

const montar = () =>
  renderEnRuta(<PostulanteDetallePage />, { patron: '/empresa/postulaciones/:id', ubicacion: '/empresa/postulaciones/p1' })

describe('PostulanteDetallePage', () => {
  it('muestra el perfil del estudiante, su CV y el historial', async () => {
    vi.mocked(api.obtenerPostulante).mockResolvedValue(postulante())
    montar()

    expect(await screen.findByRole('heading', { name: 'Ana López' })).toBeTruthy()
    expect(screen.getByText('Postulación a «Desarrollador Web»')).toBeTruthy()
    expect(screen.getByText('ana.lopez@unah.hn')).toBeTruthy()
    expect(screen.getByText('Ingeniería en Sistemas')).toBeTruthy()
    expect(screen.getByText('85')).toBeTruthy()
    expect(screen.getByText('TypeScript')).toBeTruthy()
    expect(screen.getByText('CV 2026')).toBeTruthy()
    expect(screen.getByText('Cumple requisitos')).toBeTruthy()
    expect(within(screen.getByRole('list', { name: 'Historial' })).getAllByRole('listitem')).toHaveLength(1)
    expect(api.obtenerPostulante).toHaveBeenCalledWith('p1')
  })

  it('si el estudiante no autorizó su índice, no se muestra', async () => {
    vi.mocked(api.obtenerPostulante).mockResolvedValue(
      postulante({ estudiante: { ...postulante().estudiante, indiceAcademico: null } }),
    )
    montar()
    expect(await screen.findByText('No autorizado por el estudiante')).toBeTruthy()
  })

  it('cambia el estado con los destinos permitidos y avisa', async () => {
    vi.mocked(api.obtenerPostulante).mockResolvedValue(postulante())
    vi.mocked(api.cambiarEstado).mockResolvedValue(postulante({ estado: 'EnRevision' }))
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Cambiar estado' }))
    const dialogo = within(await screen.findByRole('dialog'))
    const lista = dialogo.getByLabelText('Nuevo estado') as HTMLSelectElement
    expect(Array.from(lista.options).map((o) => o.text)).toEqual(['Seleccione…', 'En revisión', 'Entrevista', 'Rechazada'])

    await userEvent.selectOptions(lista, 'EnRevision')
    await userEvent.type(dialogo.getByLabelText('Comentario (opcional)'), 'Buen perfil')
    const guardar = dialogo.getByRole('button', { name: 'Guardar cambio' }) as HTMLButtonElement
    await waitFor(() => expect(guardar.disabled).toBe(false))
    await userEvent.click(guardar)

    await waitFor(() => expect(api.cambiarEstado).toHaveBeenCalledWith('p1', { nuevoEstado: 'EnRevision', comentario: 'Buen perfil' }))
    expect(await screen.findByText('Estado actualizado.')).toBeTruthy()
  })

  it('en un estado sin cambios disponibles (Oferta) deshabilita el botón y lo explica', async () => {
    vi.mocked(api.obtenerPostulante).mockResolvedValue(postulante({ estado: 'Oferta' }))
    montar()

    const boton = (await screen.findByRole('button', { name: 'Cambiar estado' })) as HTMLButtonElement
    expect(boton.disabled).toBe(true)
    expect(screen.getByText('No hay cambios de estado disponibles desde aquí.')).toBeTruthy()
  })

  it('"Ver currículum" abre la URL firmada en otra pestaña', async () => {
    vi.mocked(api.obtenerPostulante).mockResolvedValue(postulante())
    vi.mocked(api.urlCurriculum).mockResolvedValue({ url: 'https://almacen.example/cv.pdf?firma=abc', expiraEn: '2026-11-27T12:05:00Z' })
    const abrir = vi.spyOn(window, 'open').mockReturnValue({} as Window)
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Ver currículum' }))

    await waitFor(() => expect(abrir).toHaveBeenCalledWith('https://almacen.example/cv.pdf?firma=abc', '_blank', 'noopener,noreferrer'))
    expect(api.urlCurriculum).toHaveBeenCalledWith('p1')
  })

  it('si el navegador bloquea la ventana, avisa', async () => {
    vi.mocked(api.obtenerPostulante).mockResolvedValue(postulante())
    vi.mocked(api.urlCurriculum).mockResolvedValue({ url: 'https://almacen.example/cv.pdf', expiraEn: '2026-11-27T12:05:00Z' })
    vi.spyOn(window, 'open').mockReturnValue(null)
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Ver currículum' }))
    expect(await screen.findByText(/bloqueó la ventana/)).toBeTruthy()
  })

  it('si falla la descarga del currículum muestra el mensaje del backend', async () => {
    vi.mocked(api.obtenerPostulante).mockResolvedValue(postulante())
    vi.mocked(api.urlCurriculum).mockRejectedValue(errorHttp(404, { detail: 'El currículum ya no está disponible.' }))
    montar()

    await userEvent.click(await screen.findByRole('button', { name: 'Ver currículum' }))
    expect(await screen.findByText('El currículum ya no está disponible.')).toBeTruthy()
  })

  it('una postulación de otra empresa responde 404 y se muestra el error', async () => {
    vi.mocked(api.obtenerPostulante).mockRejectedValue(errorHttp(404, { detail: 'Postulación no encontrada.' }))
    montar()
    expect(await screen.findByText('Postulación no encontrada.')).toBeTruthy()
  })
})
