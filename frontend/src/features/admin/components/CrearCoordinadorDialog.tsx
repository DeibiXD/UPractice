import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import type { CuentaCreadaDto, UniversidadDto } from '@/shared/api/types'
import { adminApi } from '../api'
import { esquemaCrearCoordinador, type CrearCoordinadorFormulario } from '../schemas/admin'

interface Props {
  abierto: boolean
  universidades: UniversidadDto[]
  universidadInicial?: string
  onCerrar: () => void
  onCreado: (cuenta: CuentaCreadaDto) => void
}

const VACIO: CrearCoordinadorFormulario = { universidadId: '', nombre1: '', nombre2: '', apellido1: '', apellido2: '', correo: '' }

/** HU-A03: formulario de nuevo coordinador con Zod en vivo y los errores del servidor en su campo. */
export function CrearCoordinadorDialog({ abierto, universidades, universidadInicial, onCerrar, onCreado }: Props) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isValid, isSubmitting },
  } = useForm<CrearCoordinadorFormulario>({
    resolver: zodResolver(esquemaCrearCoordinador),
    mode: 'onChange',
    values: { ...VACIO, universidadId: universidadInicial ?? '' },
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
      const cuenta = await adminApi.crearCoordinador({
        ...datos,
        nombre2: datos.nombre2 || null,
        apellido2: datos.apellido2 || null,
      })
      reset()
      onCreado(cuenta)
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  const campo = (nombre: keyof CrearCoordinadorFormulario, etiqueta: string) => (
    <TextField label={etiqueta} {...register(nombre)} error={Boolean(errors[nombre])} helperText={errors[nombre]?.message} />
  )

  return (
    <Dialog open={abierto} onClose={cerrar} fullWidth maxWidth="sm">
      <form onSubmit={alEnviar} noValidate>
        <DialogTitle>Nuevo coordinador</DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {errorGeneral && <Alert severity="error">{errorGeneral}</Alert>}
            <Controller
              name="universidadId"
              control={control}
              render={({ field }) => (
                <TextField
                  select
                  label="Universidad"
                  {...field}
                  error={Boolean(errors.universidadId)}
                  helperText={errors.universidadId?.message}
                >
                  {universidades.map((u) => (
                    <MenuItem key={u.id} value={u.id}>
                      {u.siglas} · {u.nombre}
                    </MenuItem>
                  ))}
                </TextField>
              )}
            />
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              {campo('nombre1', 'Primer nombre')}
              {campo('nombre2', 'Segundo nombre (opcional)')}
            </Stack>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              {campo('apellido1', 'Primer apellido')}
              {campo('apellido2', 'Segundo apellido (opcional)')}
            </Stack>
            {campo('correo', 'Correo')}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={cerrar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" variant="contained" disabled={!isValid || isSubmitting}>
            {isSubmitting ? 'Creando…' : 'Crear coordinador'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
