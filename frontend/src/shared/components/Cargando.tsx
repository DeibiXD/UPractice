import Box from '@mui/material/Box'
import CircularProgress from '@mui/material/CircularProgress'

export function Cargando() {
  return (
    <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '50vh' }} role="status" aria-label="Cargando">
      <CircularProgress />
    </Box>
  )
}
