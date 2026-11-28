// @vitest-environment jsdom
import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { errorHttp } from '@/test/errores'
import { vacanteDetalle } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../../api'
import { VacanteDetallePage } from './VacanteDetallePage'

vi.mock('../../api', () => ({
  postulacionesApi: { obtenerVacante: vi.fn(), listarMisCurriculums: vi.fn(), postular: vi.fn() },
}))

afterEach(cleanup)
beforeEach(() => vi.clearAllMocks())

const montar = () =>
  renderEnRuta(<VacanteDetallePage />, { patron: '/estudiante/vacantes/:id', ubicacion: '/estudiante/vacantes/v1' })

describe('VacanteDetallePage', () => {
  it('muestra el detalle y ofrece Postular', async () => {
    vi.mocked(api.obtenerVacante).mockResolvedValue(vacanteDetalle())
    montar()

    expect(await screen.findByRole('heading', { name: 'Desarrollador Web' })).toBeTruthy()
    expect(screen.getByText('Digitales S.A.')).toBeTruthy()
    expect(screen.getByText('Híbrida')).toBeTruthy()
    expect(screen.getByText('Ingeniería en Sistemas')).toBeTruthy()
    expect(screen.getByText('TypeScript (requerida)')).toBeTruthy()
    expect(screen.getByText('Docker')).toBeTruthy()
    expect(screen.getByText(/Desarrollo de aplicaciones web/)).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Postular' })).toBeTruthy()
    expect(api.obtenerVacante).toHaveBeenCalledWith('v1')
  })

  it('si ya se postuló no muestra el botón Postular', async () => {
    vi.mocked(api.obtenerVacante).mockResolvedValue(vacanteDetalle({ yaPostulado: true }))
    montar()

    expect(await screen.findByText('Ya se postuló')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Postular' })).toBeNull()
  })

  it('si no cumple todos los requisitos avisa, pero deja postular', async () => {
    vi.mocked(api.obtenerVacante).mockResolvedValue(vacanteDetalle({ cumploRequisitos: false }))
    montar()

    expect(await screen.findByText(/no cumple todos los requisitos deseables/)).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Postular' })).toBeTruthy()
  })

  it('una vacante que ya no está disponible muestra el mensaje del backend', async () => {
    vi.mocked(api.obtenerVacante).mockRejectedValue(errorHttp(404, { detail: 'La vacante no está disponible.' }))
    montar()
    expect(await screen.findByText('La vacante no está disponible.')).toBeTruthy()
  })
})
