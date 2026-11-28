import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import FormHelperText from '@mui/material/FormHelperText'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import type { CurriculumDto } from '@/shared/api/types'
import { curriculumsApi } from '../api'
import { esquemaSubirCurriculum, type SubirCurriculumFormulario } from '../schemas/curriculum'

interface Props {
  abierto: boolean
  onCerrar: () => void
  onSubido: (curriculum: CurriculumDto) => void
}

/** Formulario de subida (HU-03): nombre de la versión y PDF de hasta 5 MB, validados en vivo con Zod. */
export function SubirCurriculumDialog({ abierto, onCerrar, onSubido }: Props) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isValid, isSubmitting },
  } = useForm<SubirCurriculumFormulario>({
    resolver: zodResolver(esquemaSubirCurriculum),
    mode: 'onChange',
    defaultValues: { nombreVersion: '' },
  })

  const cerrar = () => {
    if (isSubmitting) return
    reset()
    setErrorGeneral(null)
    onCerrar()
  }

  const alEnviar = handleSubmit(async (datos) => {
    setErrorGeneral(null)
    try {
      const creado = await curriculumsApi.subir(datos.nombreVersion, datos.archivo)
      reset()
      onSubido(creado)
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  return (
    <Dialog open={abierto} onClose={cerrar} fullWidth maxWidth="sm">
      <form onSubmit={alEnviar} noValidate>
        <DialogTitle>Subir versión del CV</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {errorGeneral && <Alert severity="error">{errorGeneral}</Alert>}
            <TextField
              label="Nombre de la versión"
              placeholder="Ej. CV desarrollo web"
              autoFocus
              {...register('nombreVersion')}
              error={Boolean(errors.nombreVersion)}
              helperText={errors.nombreVersion?.message}
            />
            <Controller
              name="archivo"
              control={control}
              render={({ field }) => (
                <Stack spacing={0.5}>
                  <Button variant="outlined" component="label">
                    {field.value ? 'Cambiar archivo' : 'Elegir PDF'}
                    <input
                      hidden
                      type="file"
                      accept="application/pdf,.pdf"
                      onChange={(e) => field.onChange(e.target.files?.[0])}
                    />
                  </Button>
                  {field.value && (
                    <Typography variant="body2" color="text.secondary">
                      {field.value.name} · {(field.value.size / 1024).toFixed(0)} KB
                    </Typography>
                  )}
                  <FormHelperText error={Boolean(errors.archivo)}>
                    {errors.archivo?.message ?? 'PDF de máximo 5 MB.'}
                  </FormHelperText>
                </Stack>
              )}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={cerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={!isValid || isSubmitting}>
            {isSubmitting ? 'Subiendo…' : 'Subir'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
