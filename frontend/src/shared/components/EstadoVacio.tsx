import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'

interface Props {
  titulo: string
  descripcion?: string
  /** Botón o enlace sugerido (p. ej. "Crear la primera vacante"). */
  accion?: ReactNode
}

/** Para listas sin resultados. */
export function EstadoVacio({ titulo, descripcion, accion }: Props) {
  return (
    <Paper variant="outlined" sx={{ p: 5 }}>
      <Stack spacing={1.5} sx={{ alignItems: 'center', textAlign: 'center' }}>
        <Typography variant="h6">{titulo}</Typography>
        {descripcion && <Typography color="text.secondary">{descripcion}</Typography>}
        {accion}
      </Stack>
    </Paper>
  )
}
