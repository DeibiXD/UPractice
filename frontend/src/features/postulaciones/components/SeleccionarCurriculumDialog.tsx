import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import TextField from '@mui/material/TextField'
import { useEffect, useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import { EstadoError } from '@/shared/components/EstadoError'
import { useMisCurriculums } from '../hooks'
import { esquemaSeleccionCurriculum, type SeleccionCurriculumFormulario } from '../schemas/postulaciones'

interface Props {
  abierto: boolean
  titulo: string
  descripcion?: ReactNode
  textoConfirmar: string
  /** Acción que recibe el currículum elegido (postular o reactivar). Si lanza, el error se muestra aquí. */
  enviar: (curriculumId: string) => Promise<unknown>
  onCerrar: () => void
  onExito?: () => void
}

/** Diálogo para elegir con qué currículum se postula o se reactiva una postulación. */
export function SeleccionarCurriculumDialog({ abierto, ...resto }: Props) {
  // El formulario se monta solo mientras el diálogo está abierto: así su estado se reinicia en cada apertura.
  return abierto ? <SeleccionarCurriculumFormulario {...resto} /> : null
}

function SeleccionarCurriculumFormulario({
  titulo,
  descripcion,
  textoConfirmar,
  enviar,
  onCerrar,
  onExito,
}: Omit<Props, 'abierto'>) {
  const curriculums = useMisCurriculums(true)
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<SeleccionCurriculumFormulario>({
    resolver: zodResolver(esquemaSeleccionCurriculum),
    defaultValues: { curriculumId: '' },
  })

  // Al abrir, se preselecciona el currículum principal (o el primero).
  useEffect(() => {
    if (!curriculums.data) return
    const preferido = curriculums.data.find((cv) => cv.esPrincipal) ?? curriculums.data[0]
    if (preferido) setValue('curriculumId', preferido.id)
  }, [curriculums.data, setValue])

  const alEnviar = handleSubmit(async ({ curriculumId }) => {
    setErrorGeneral(null)
    try {
      await enviar(curriculumId)
      onExito?.()
      onCerrar()
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  const sinCurriculums = curriculums.isSuccess && curriculums.data.length === 0

  return (
    <Dialog open onClose={isSubmitting ? undefined : onCerrar} maxWidth="xs" fullWidth>
      <form onSubmit={alEnviar} noValidate>
        <DialogTitle>{titulo}</DialogTitle>
        <DialogContent>
          {descripcion && <DialogContentText sx={{ mb: 2 }}>{descripcion}</DialogContentText>}

          {errorGeneral && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {errorGeneral}
            </Alert>
          )}

          {curriculums.isPending && (
            <Box sx={{ display: 'grid', placeItems: 'center', py: 3 }}>
              <CircularProgress size={28} aria-label="Cargando currículums" />
            </Box>
          )}

          {curriculums.isError && (
            <EstadoError
              mensaje={extraerError(curriculums.error).mensaje}
              onReintentar={() => void curriculums.refetch()}
            />
          )}

          {sinCurriculums && (
            <Alert severity="info">
              Aún no tiene currículums. Suba uno en “Mis currículums” para poder postularse.
            </Alert>
          )}

          {curriculums.isSuccess && curriculums.data.length > 0 && (
            <TextField
              select
              fullWidth
              label="Currículum"
              slotProps={{ select: { native: true } }}
              {...register('curriculumId')}
              error={Boolean(errors.curriculumId)}
              helperText={errors.curriculumId?.message}
            >
              <option value="">Seleccione…</option>
              {curriculums.data.map((cv) => (
                <option key={cv.id} value={cv.id}>
                  {cv.nombreVersion}
                  {cv.esPrincipal ? ' (principal)' : ''}
                </option>
              ))}
            </TextField>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={onCerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting || !curriculums.isSuccess || sinCurriculums}>
            {isSubmitting ? 'Enviando…' : textoConfirmar}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
