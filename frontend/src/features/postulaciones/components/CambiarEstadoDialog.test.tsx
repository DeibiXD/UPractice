// @vitest-environment jsdom
import { cleanup, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { EstadoPostulacion } from '@/shared/api/types'
import { errorHttp } from '@/test/errores'
import { renderEnRuta } from '@/test/render'
import { CambiarEstadoDialog } from './CambiarEstadoDialog'

afterEach(cleanup)

function montar(estadoActual: EstadoPostulacion, enviar = vi.fn().mockResolvedValue(undefined)) {
  const onCerrar = vi.fn()
  const onExito = vi.fn()
  renderEnRuta(
    <CambiarEstadoDialog
      abierto
      estadoActual={estadoActual}
      nombreEstudiante="Ana López"
      enviar={enviar}
      onCerrar={onCerrar}
      onExito={onExito}
    />,
  )
  return { enviar, onCerrar, onExito }
}

const opciones = () =>
  Array.from((screen.getByLabelText('Nuevo estado') as HTMLSelectElement).options).map((o) => o.text)

describe('CambiarEstadoDialog', () => {
  it('desde Enviada ofrece solo los destinos permitidos', () => {
    montar('Enviada')
    expect(opciones()).toEqual(['Seleccione…', 'En revisión', 'Entrevista', 'Rechazada'])
  })

  it('desde En revisión ofrece Entrevista, Oferta y Rechazada', () => {
    montar('EnRevision')
    expect(opciones()).toEqual(['Seleccione…', 'Entrevista', 'Oferta', 'Rechazada'])
  })

  it('desde Entrevista ofrece Oferta y Rechazada', () => {
    montar('Entrevista')
    expect(opciones()).toEqual(['Seleccione…', 'Oferta', 'Rechazada'])
  })

  it('no permite guardar hasta elegir un estado y envía estado y comentario', async () => {
    const { enviar, onCerrar, onExito } = montar('Enviada')
    const guardar = screen.getByRole('button', { name: 'Guardar cambio' }) as HTMLButtonElement
    expect(guardar.disabled).toBe(true)

    await userEvent.selectOptions(screen.getByLabelText('Nuevo estado'), 'EnRevision')
    await userEvent.type(screen.getByLabelText('Comentario (opcional)'), '  Buen perfil  ')
    await waitFor(() => expect(guardar.disabled).toBe(false))
    await userEvent.click(guardar)

    await waitFor(() => expect(enviar).toHaveBeenCalledWith({ nuevoEstado: 'EnRevision', comentario: 'Buen perfil' }))
    expect(onExito).toHaveBeenCalled()
    expect(onCerrar).toHaveBeenCalled()
  })

  it('el comentario vacío se envía como null', async () => {
    const { enviar } = montar('Enviada')
    await userEvent.selectOptions(screen.getByLabelText('Nuevo estado'), 'Entrevista')
    const guardar = screen.getByRole('button', { name: 'Guardar cambio' }) as HTMLButtonElement
    await waitFor(() => expect(guardar.disabled).toBe(false))
    await userEvent.click(guardar)

    await waitFor(() => expect(enviar).toHaveBeenCalledWith({ nuevoEstado: 'Entrevista', comentario: null }))
  })

  it('rechazar advierte que no se puede deshacer', async () => {
    montar('Enviada')
    await userEvent.selectOptions(screen.getByLabelText('Nuevo estado'), 'Rechazada')
    expect(await screen.findByText(/no se puede deshacer/)).toBeTruthy()
  })

  it('un comentario de más de 500 caracteres bloquea el guardado y avisa', async () => {
    montar('Enviada')
    await userEvent.selectOptions(screen.getByLabelText('Nuevo estado'), 'EnRevision')
    const comentario = screen.getByLabelText('Comentario (opcional)')
    await userEvent.click(comentario)
    await userEvent.paste('a'.repeat(501))

    expect(await screen.findByText('Máximo 500 caracteres.')).toBeTruthy()
    expect((screen.getByRole('button', { name: 'Guardar cambio' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('si el backend responde 422 TRANSICION_INVALIDA muestra su mensaje y no cierra', async () => {
    const enviar = vi
      .fn()
      .mockRejectedValue(errorHttp(422, { detail: 'No se puede pasar de Enviada a Oferta.', code: 'TRANSICION_INVALIDA' }))
    const { onCerrar } = montar('Enviada', enviar)

    await userEvent.selectOptions(screen.getByLabelText('Nuevo estado'), 'EnRevision')
    const guardar = screen.getByRole('button', { name: 'Guardar cambio' }) as HTMLButtonElement
    await waitFor(() => expect(guardar.disabled).toBe(false))
    await userEvent.click(guardar)

    expect(await screen.findByText('No se puede pasar de Enviada a Oferta.')).toBeTruthy()
    expect(onCerrar).not.toHaveBeenCalled()
  })
})
