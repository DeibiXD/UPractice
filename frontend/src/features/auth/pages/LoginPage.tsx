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
import { Navigate, useLocation } from 'react-router-dom'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import { rutaInicio } from '@/shared/auth/rutaInicio'
import { useAuth } from '@/shared/auth/useAuth'
import { esquemaLogin, type LoginFormulario } from '../schemas/auth'

export function LoginPage() {
  const { usuario, login } = useAuth()
  const ubicacion = useLocation()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<LoginFormulario>({
    resolver: zodResolver(esquemaLogin),
    mode: 'onChange',
    defaultValues: { correo: '', password: '' },
  })

  // Ya hay sesión (o se acaba de iniciar): se redirige de forma declarativa.
  if (usuario) {
    const desde = (ubicacion.state as { desde?: string } | null)?.desde
    const destino = usuario.debeCambiarPassword ? '/cambiar-password' : (desde ?? rutaInicio(usuario.rol))
    return <Navigate to={destino} replace />
  }

  const alEnviar = handleSubmit(async (datos) => {
    setErrorGeneral(null)
    try {
      await login(datos)
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      // Si el backend señaló campos, el error ya está pintado en ellos; si no, va en el Alert.
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }} elevation={3}>
        <Stack component="form" spacing={2.5} onSubmit={alEnviar} noValidate>
          <Typography variant="h5" component="h1">
            Iniciar sesión
          </Typography>

          {errorGeneral && <Alert severity="error">{errorGeneral}</Alert>}

          <TextField
            label="Correo"
            type="email"
            autoComplete="username"
            autoFocus
            {...register('correo')}
            error={Boolean(errors.correo)}
            helperText={errors.correo?.message}
          />
          <TextField
            label="Contraseña"
            type="password"
            autoComplete="current-password"
            {...register('password')}
            error={Boolean(errors.password)}
            helperText={errors.password?.message}
          />

          <Button type="submit" variant="contained" size="large" disabled={!isValid || isSubmitting}>
            {isSubmitting ? 'Ingresando…' : 'Ingresar'}
          </Button>
        </Stack>
      </Paper>
    </Container>
  )
}
