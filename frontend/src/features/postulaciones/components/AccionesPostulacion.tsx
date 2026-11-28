import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { extraerError } from '@/shared/api/errores'
import type { AccionPostulacion, MiPostulacionDetalleDto } from '@/shared/api/types'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { ETIQUETA_ACCION, ordenarAcciones } from '../estados'
import { useResponderOferta, useRetirar } from '../hooks'
import { ReactivarDialog } from './PostularDialog'

interface Confirmacion {
  titulo: string
  texto: string
  boton: string
  peligro: boolean
}

const CONFIRMACIONES: Record<'Retirar' | 'AceptarOferta' | 'DeclinarOferta', Confirmacion> = {
  Retirar: {
    titulo: 'Retirar postulación',
    texto: 'La empresa dejará de considerarte para esta vacante. Podrás reactivarla mientras siga abierta.',
    boton: 'Retirar',
    peligro: true,
  },
  AceptarOferta: {
    titulo: 'Aceptar oferta',
    texto: 'Al aceptar, quedas asignado a esta práctica. Esta decisión es final.',
    boton: 'Aceptar oferta',
    peligro: false,
  },
  DeclinarOferta: {
    titulo: 'Declinar oferta',
    texto: 'Si declinas, la postulación se cierra y no se puede reactivar. Esta decisión es final.',
    boton: 'Declinar oferta',
    peligro: true,
  },
}

/** Un botón por cada acción que la API dice que el estudiante puede hacer ahora (HU-06). */
export function AccionesPostulacion({ postulacion }: { postulacion: MiPostulacionDetalleDto }) {
  const [abierta, setAbierta] = useState<AccionPostulacion | null>(null)
  const retirar = useRetirar(postulacion.id)
  const responder = useResponderOferta(postulacion.id)

  const acciones = ordenarAcciones(postulacion.acciones)
  if (acciones.length === 0) return null

  const cerrar = () => setAbierta(null)
  const confirmable = abierta && abierta !== 'Reactivar' ? CONFIRMACIONES[abierta] : null
  const mutacion = abierta === 'Retirar' ? retirar : responder

  const confirmar = async () => {
    if (!abierta || abierta === 'Reactivar') return
    try {
      if (abierta === 'Retirar') await retirar.mutateAsync()
      else await responder.mutateAsync(abierta === 'AceptarOferta')
      cerrar()
    } catch {
      // El error queda en mutacion.error y se muestra dentro del diálogo.
    }
  }

  return (
    <>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
        {acciones.map((accion) => (
          <Button
            key={accion}
            variant={accion === 'AceptarOferta' || accion === 'Reactivar' ? 'contained' : 'outlined'}
            color={accion === 'Retirar' || accion === 'DeclinarOferta' ? 'error' : 'primary'}
            onClick={() => {
              retirar.reset()
              responder.reset()
              setAbierta(accion)
            }}
          >
            {ETIQUETA_ACCION[accion]}
          </Button>
        ))}
      </Stack>

      <ConfirmDialog
        abierto={confirmable !== null}
        titulo={confirmable?.titulo ?? ''}
        mensaje={
          <Stack spacing={2}>
            <span>{confirmable?.texto}</span>
            {mutacion.isError && <Alert severity="error">{extraerError(mutacion.error).mensaje}</Alert>}
          </Stack>
        }
        textoConfirmar={confirmable?.boton}
        peligro={confirmable?.peligro}
        cargando={mutacion.isPending}
        onConfirmar={confirmar}
        onCancelar={cerrar}
      />

      <ReactivarDialog
        postulacionId={postulacion.id}
        vacanteTitulo={postulacion.vacante.titulo}
        abierto={abierta === 'Reactivar'}
        onCerrar={cerrar}
      />
    </>
  )
}
