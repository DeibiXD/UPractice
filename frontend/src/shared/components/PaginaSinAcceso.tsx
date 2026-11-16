import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Link } from 'react-router-dom'

export function PaginaSinAcceso() {
  return (
    <Stack spacing={2} sx={{ alignItems: 'center', py: 8 }}>
      <Typography variant="h5">Sin acceso</Typography>
      <Typography color="text.secondary">Su rol no tiene permiso para ver esta pantalla.</Typography>
      <Button component={Link} to="/" variant="contained">
        Volver al inicio
      </Button>
    </Stack>
  )
}
