import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import type { EstudianteVerificacionDto } from '@/shared/api/types'
import { verificacionApi } from '../api'
import { esquemaRechazoVerificacion, type RechazoVerificacionFormulario } from '../schemas/verificacion'

interface Props {
  estudiante: EstudianteVerificacionDto | null
  onCerrar: () => void
  onRechazado: () => void
}

/** Rechazo con observación obligatoria (HU-C02). El estudiante la ve en su perfil y puede corregir. */
export function RechazarVerificacionDialog({ estudiante, onCerrar, onRechazado }: Props) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isValid, isSubmitting },
  } = useForm<RechazoVerificacionFormulario>({
    resolver: zodResolver(esquemaRechazoVerificacion),
    mode: 'onChange',
    defaultValues: { observacion: '' },
  })

  const cerrar = () => {
    if (isSubmitting) return
    reset()
    setErrorGeneral(null)
    onCerrar()
  }

  const alEnviar = handleSubmit(async (datos) => {
    if (!estudiante) return
    setErrorGeneral(null)
    try {
      await verificacionApi.resolver(estudiante.usuarioId, { aprobar: false, observacion: datos.observacion })
      reset()
      onRechazado()
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  return (
    <Dialog open={estudiante !== null} onClose={cerrar} fullWidth maxWidth="sm">
      <form onSubmit={alEnviar} noValidate>
        <DialogTitle>Rechazar perfil</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            {estudiante?.nombreCompleto} verá esta observación y podrá corregir sus datos para volver a solicitar la verificación.
          </DialogContentText>
          {errorGeneral && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {errorGeneral}
            </Alert>
          )}
          <TextField
            label="Observación"
            multiline
            minRows={3}
            fullWidth
            autoFocus
            {...register('observacion')}
            error={Boolean(errors.observacion)}
            helperText={errors.observacion?.message}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={cerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" color="error" variant="contained" disabled={!isValid || isSubmitting}>
            {isSubmitting ? 'Rechazando…' : 'Rechazar'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
