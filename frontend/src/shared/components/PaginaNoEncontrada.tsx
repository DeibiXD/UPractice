import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Link } from 'react-router-dom'

export function PaginaNoEncontrada() {
  return (
    <Stack spacing={2} sx={{ alignItems: 'center', py: 8 }}>
      <Typography variant="h5">Página no encontrada</Typography>
      <Typography color="text.secondary">La dirección que abrió no existe.</Typography>
      <Button component={Link} to="/" variant="contained">
        Ir al inicio
      </Button>
    </Stack>
  )
}
