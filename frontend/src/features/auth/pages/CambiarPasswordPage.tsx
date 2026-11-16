import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Container from '@mui/material/Container'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import { rutaInicio } from '@/shared/auth/rutaInicio'
import { useAuth } from '@/shared/auth/useAuth'
import { esquemaCambiarPassword, type CambiarPasswordFormulario } from '../schemas/auth'

export function CambiarPasswordPage() {
  const { usuario, cambiarPassword, logout } = useAuth()
  const navigate = useNavigate()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<CambiarPasswordFormulario>({
    resolver: zodResolver(esquemaCambiarPassword),
    mode: 'onChange',
    defaultValues: { passwordActual: '', passwordNuevo: '', confirmarPassword: '' },
  })

  const alEnviar = handleSubmit(async (datos) => {
    setErrorGeneral(null)
    try {
      const actualizado = await cambiarPassword(datos)
      navigate(rutaInicio(actualizado.rol), { replace: true })
    } catch (causa) {
      const error = extraerError(causa)
      // Los errores semánticos con campo (passwordActual incorrecta, nueva igual a la actual) llegan en 400.
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  const salir = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }} elevation={3}>
        <Stack component="form" spacing={2.5} onSubmit={alEnviar} noValidate>
          <Typography variant="h5" component="h1">
            Cambiar contraseña
          </Typography>

          {usuario?.debeCambiarPassword && (
            <Alert severity="info">Debe cambiar su contraseña temporal antes de continuar.</Alert>
          )}
          {errorGeneral && <Alert severity="error">{errorGeneral}</Alert>}

          <TextField
            label="Contraseña actual"
            type="password"
            autoComplete="current-password"
            {...register('passwordActual')}
            error={Boolean(errors.passwordActual)}
            helperText={errors.passwordActual?.message}
          />
          <TextField
            label="Contraseña nueva"
            type="password"
            autoComplete="new-password"
            {...register('passwordNuevo')}
            error={Boolean(errors.passwordNuevo)}
            helperText={errors.passwordNuevo?.message}
          />
          <TextField
            label="Confirmar contraseña nueva"
            type="password"
            autoComplete="new-password"
            {...register('confirmarPassword')}
            error={Boolean(errors.confirmarPassword)}
            helperText={errors.confirmarPassword?.message}
          />

          <Button type="submit" variant="contained" size="large" disabled={!isValid || isSubmitting}>
            {isSubmitting ? 'Guardando…' : 'Guardar contraseña'}
          </Button>
          <Button type="button" color="inherit" onClick={salir}>
            Cerrar sesión
          </Button>
        </Stack>
      </Paper>
    </Container>
  )
}
