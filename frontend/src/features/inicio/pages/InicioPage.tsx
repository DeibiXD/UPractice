import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useAuth } from '@/shared/auth/useAuth'

/** Pantalla provisional: cada rol la reemplaza por su pantalla principal (ver shared/auth/rutaInicio.ts). */
export function InicioPage() {
  const { usuario } = useAuth()
  return (
    <Paper sx={{ p: 4 }}>
      <Stack spacing={1}>
        <Typography variant="h5" component="h1">
          Hola, {usuario?.nombreCompleto}
        </Typography>
        <Typography color="text.secondary">
          Sesión iniciada como {usuario?.rol} ({usuario?.correo}).
        </Typography>
        {usuario?.empresa && <Typography color="text.secondary">Empresa: {usuario.empresa.nombre}</Typography>}
        {usuario?.universidad && (
          <Typography color="text.secondary">Universidad: {usuario.universidad.nombre}</Typography>
        )}
      </Stack>
    </Paper>
  )
}
