// @vitest-environment jsdom
import { cleanup, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { CV_PRINCIPAL, miPostulacion } from '@/test/fixtures'
import { renderEnRuta } from '@/test/render'
import { postulacionesApi as api } from '../api'
import { BotonPostular } from './BotonPostular'

vi.mock('../api', () => ({ postulacionesApi: { listarMisCurriculums: vi.fn(), postular: vi.fn() } }))

afterEach(cleanup)
beforeEach(() => vi.clearAllMocks())

describe('BotonPostular', () => {
  it('si ya se postuló, no ofrece postular y enlaza a Mis postulaciones', () => {
    renderEnRuta(<BotonPostular vacanteId="v1" vacanteTitulo="Desarrollador Web" yaPostulado />)

    expect(screen.getByText('Ya se postuló')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Postular' })).toBeNull()
    expect(screen.getByRole('link', { name: 'Ver mis postulaciones' }).getAttribute('href')).toBe(
      '/estudiante/postulaciones',
    )
  })

  it('abre el diálogo, envía la postulación con el currículum elegido y avisa', async () => {
    vi.mocked(api.listarMisCurriculums).mockResolvedValue([CV_PRINCIPAL])
    vi.mocked(api.postular).mockResolvedValue(miPostulacion())
    renderEnRuta(<BotonPostular vacanteId="v1" vacanteTitulo="Desarrollador Web" yaPostulado={false} />)

    await userEvent.click(screen.getByRole('button', { name: 'Postular' }))
    expect(await screen.findByText('Postularse a «Desarrollador Web»')).toBeTruthy()
    await screen.findByLabelText('Currículum')
    await userEvent.click(screen.getByRole('button', { name: 'Enviar postulación' }))

    await waitFor(() => expect(api.postular).toHaveBeenCalledWith('v1', { curriculumId: 'cv-1' }))
    expect(await screen.findByText('Postulación enviada.')).toBeTruthy()
  })
})
