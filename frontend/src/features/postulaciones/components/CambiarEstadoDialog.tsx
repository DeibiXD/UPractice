import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import type { CambiarEstadoPostulacionRequest, EstadoPostulacion } from '@/shared/api/types'
import { ETIQUETA_ESTADO, siguientesEmpresa } from '../lib/transiciones'
import { esquemaCambioEstado, type CambioEstadoFormulario } from '../schemas/postulaciones'

interface Props {
  abierto: boolean
  estadoActual: EstadoPostulacion
  nombreEstudiante: string
  enviar: (datos: CambiarEstadoPostulacionRequest) => Promise<unknown>
  onCerrar: () => void
  onExito?: () => void
}

/** Cambio de estado de una postulación por la empresa (HU-E07): solo ofrece los destinos que el backend permite. */
export function CambiarEstadoDialog({ abierto, ...resto }: Props) {
  // El formulario se monta solo mientras el diálogo está abierto: así su estado se reinicia en cada apertura.
  return abierto ? <CambiarEstadoFormulario {...resto} /> : null
}

function CambiarEstadoFormulario({
  estadoActual,
  nombreEstudiante,
  enviar,
  onCerrar,
  onExito,
}: Omit<Props, 'abierto'>) {
  const destinos = siguientesEmpresa(estadoActual)
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    control,
    formState: { errors, isValid, isSubmitting },
  } = useForm<CambioEstadoFormulario>({
    resolver: zodResolver(esquemaCambioEstado),
    mode: 'onChange',
    defaultValues: { nuevoEstado: '', comentario: '' },
  })

  const nuevoEstado = useWatch({ control, name: 'nuevoEstado' })
  const comentario = useWatch({ control, name: 'comentario' })

  const alEnviar = handleSubmit(async (datos) => {
    setErrorGeneral(null)
    const destino = destinos.find((estado) => estado === datos.nuevoEstado)
    if (!destino) {
      setError('nuevoEstado', { type: 'validate', message: 'Seleccione el nuevo estado.' })
      return
    }
    try {
      await enviar({ nuevoEstado: destino, comentario: datos.comentario.trim() || null })
      onExito?.()
      onCerrar()
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  return (
    <Dialog open onClose={isSubmitting ? undefined : onCerrar} maxWidth="xs" fullWidth>
      <form onSubmit={alEnviar} noValidate>
        <DialogTitle>Cambiar estado</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Postulación de {nombreEstudiante}. Estado actual: {ETIQUETA_ESTADO[estadoActual]}.
          </DialogContentText>

          <Stack spacing={2}>
            {errorGeneral && <Alert severity="error">{errorGeneral}</Alert>}

            <TextField
              select
              label="Nuevo estado"
              slotProps={{ select: { native: true } }}
              {...register('nuevoEstado')}
              error={Boolean(errors.nuevoEstado)}
              helperText={errors.nuevoEstado?.message}
            >
              <option value="">Seleccione…</option>
              {destinos.map((estado) => (
                <option key={estado} value={estado}>
                  {ETIQUETA_ESTADO[estado]}
                </option>
              ))}
            </TextField>

            <TextField
              label="Comentario (opcional)"
              multiline
              minRows={3}
              {...register('comentario')}
              error={Boolean(errors.comentario)}
              helperText={errors.comentario?.message ?? `${comentario.length}/500`}
            />

            {nuevoEstado === 'Rechazada' && (
              <Alert severity="warning">
                El estudiante verá que su postulación fue rechazada. Esta acción no se puede deshacer.
              </Alert>
            )}
            {nuevoEstado === 'Oferta' && (
              <Alert severity="info">
                El estudiante podrá aceptar o declinar la oferta desde su cuenta.
              </Alert>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onCerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button
            type="submit"
            variant="contained"
            color={nuevoEstado === 'Rechazada' ? 'error' : 'primary'}
            disabled={!isValid || isSubmitting}
          >
            {isSubmitting ? 'Guardando…' : 'Guardar cambio'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
