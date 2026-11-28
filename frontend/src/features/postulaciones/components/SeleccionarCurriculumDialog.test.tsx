// @vitest-environment jsdom
import { cleanup, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { CV_PRINCIPAL, CV_SECUNDARIO } from '@/test/fixtures'
import { errorHttp } from '@/test/errores'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../api'
import { SeleccionarCurriculumDialog } from './SeleccionarCurriculumDialog'

vi.mock('../api', () => ({ postulacionesApi: { listarMisCurriculums: vi.fn() } }))

afterEach(cleanup)
beforeEach(() => vi.clearAllMocks())

function montar(enviar = vi.fn().mockResolvedValue(undefined)) {
  const onCerrar = vi.fn()
  const onExito = vi.fn()
  renderEnRuta(
    <SeleccionarCurriculumDialog
      abierto
      titulo="Postularse"
      textoConfirmar="Enviar"
      enviar={enviar}
      onCerrar={onCerrar}
      onExito={onExito}
    />,
  )
  return { enviar, onCerrar, onExito }
}

describe('SeleccionarCurriculumDialog', () => {
  it('lista los currículums, preselecciona el principal y envía el elegido', async () => {
    vi.mocked(api.listarMisCurriculums).mockResolvedValue([CV_SECUNDARIO, CV_PRINCIPAL])
    const { enviar, onCerrar, onExito } = montar()

    const lista = (await screen.findByLabelText('Currículum')) as HTMLSelectElement
    await waitFor(() => expect(lista.value).toBe('cv-1'))
    expect(Array.from(lista.options).map((o) => o.text)).toEqual(['Seleccione…', 'CV en inglés', 'CV 2026 (principal)'])

    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    await waitFor(() => expect(enviar).toHaveBeenCalledWith('cv-1'))
    expect(onExito).toHaveBeenCalled()
    expect(onCerrar).toHaveBeenCalled()
  })

  it('permite elegir otro currículum', async () => {
    vi.mocked(api.listarMisCurriculums).mockResolvedValue([CV_PRINCIPAL, CV_SECUNDARIO])
    const { enviar } = montar()

    const lista = await screen.findByLabelText('Currículum')
    await userEvent.selectOptions(lista, 'cv-2')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    await waitFor(() => expect(enviar).toHaveBeenCalledWith('cv-2'))
  })

  it('si el backend rechaza (422), muestra su mensaje y no cierra', async () => {
    vi.mocked(api.listarMisCurriculums).mockResolvedValue([CV_PRINCIPAL])
    const enviar = vi
      .fn()
      .mockRejectedValue(errorHttp(422, { detail: 'Su perfil académico debe estar verificado.', code: 'PERFIL_NO_VERIFICADO' }))
    const { onCerrar, onExito } = montar(enviar)

    await screen.findByLabelText('Currículum')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(await screen.findByText('Su perfil académico debe estar verificado.')).toBeTruthy()
    expect(onCerrar).not.toHaveBeenCalled()
    expect(onExito).not.toHaveBeenCalled()
  })

  it('un error de campo del backend (400) se pinta en el selector', async () => {
    vi.mocked(api.listarMisCurriculums).mockResolvedValue([CV_PRINCIPAL])
    const enviar = vi
      .fn()
      .mockRejectedValue(errorHttp(400, { title: 'Uno o más campos no son válidos.', errors: { curriculumId: ['El currículum no existe.'] } }))
    montar(enviar)

    await screen.findByLabelText('Currículum')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar' }))

    expect(await screen.findByText('El currículum no existe.')).toBeTruthy()
  })

  it('sin currículums avisa y no deja enviar', async () => {
    vi.mocked(api.listarMisCurriculums).mockResolvedValue([])
    montar()

    expect(await screen.findByText(/Aún no tiene currículums/)).toBeTruthy()
    expect((screen.getByRole('button', { name: 'Enviar' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('si falla la carga de currículums muestra el error y permite reintentar', async () => {
    vi.mocked(api.listarMisCurriculums).mockRejectedValue(errorHttp(404, {}))
    montar()

    expect(await screen.findByText('No se encontró el recurso.')).toBeTruthy()
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))
    await waitFor(() => expect(api.listarMisCurriculums).toHaveBeenCalledTimes(2))
  })
})
