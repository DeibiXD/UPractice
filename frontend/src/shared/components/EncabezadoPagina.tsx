import Box from '@mui/material/Box'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'

interface Props {
  titulo: string
  subtitulo?: string
  /** Botones de la derecha (p. ej. "Nueva vacante"). */
  acciones?: ReactNode
}

export function EncabezadoPagina({ titulo, subtitulo, acciones }: Props) {
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={2}
      sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' }, mb: 3 }}
    >
      <Box>
        <Typography variant="h5" component="h1">
          {titulo}
        </Typography>
        {subtitulo && <Typography color="text.secondary">{subtitulo}</Typography>}
      </Box>
      {acciones && <Stack direction="row" spacing={1}>{acciones}</Stack>}
    </Stack>
  )
}
