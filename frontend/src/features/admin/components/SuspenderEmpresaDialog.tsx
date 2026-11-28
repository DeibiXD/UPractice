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
import type { EmpresaAdminDto } from '@/shared/api/types'
import { adminApi } from '../api'
import { esquemaSuspenderEmpresa, type SuspenderEmpresaFormulario } from '../schemas/admin'

interface Props {
  empresa: EmpresaAdminDto | null
  onCerrar: () => void
  onSuspendida: () => void
}

/** HU-A04: suspensión con motivo obligatorio. */
export function SuspenderEmpresaDialog({ empresa, onCerrar, onSuspendida }: Props) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isValid, isSubmitting },
  } = useForm<SuspenderEmpresaFormulario>({
    resolver: zodResolver(esquemaSuspenderEmpresa),
    mode: 'onChange',
    defaultValues: { motivoSuspension: '' },
  })

  const cerrar = () => {
    if (isSubmitting) return
    reset()
    setErrorGeneral(null)
    onCerrar()
  }

  const alEnviar = handleSubmit(async (datos) => {
    if (!empresa) return
    setErrorGeneral(null)
    try {
      await adminApi.cambiarEstadoEmpresa(empresa.id, { activa: false, motivoSuspension: datos.motivoSuspension })
      reset()
      onSuspendida()
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  return (
    <Dialog open={empresa !== null} onClose={cerrar} fullWidth maxWidth="sm">
      <form onSubmit={alEnviar} noValidate>
        <DialogTitle>Suspender {empresa?.nombreComercial}</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Mientras esté suspendida, sus usuarios no podrán iniciar sesión y sus vacantes dejarán de verse para los estudiantes.
            Puede reactivarla cuando quiera.
          </DialogContentText>
          {errorGeneral && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {errorGeneral}
            </Alert>
          )}
          <TextField
            label="Motivo de la suspensión"
            multiline
            minRows={3}
            fullWidth
            autoFocus
            {...register('motivoSuspension')}
            error={Boolean(errors.motivoSuspension)}
            helperText={errors.motivoSuspension?.message}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={cerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" color="error" variant="contained" disabled={!isValid || isSubmitting}>
            {isSubmitting ? 'Suspendiendo…' : 'Suspender'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
