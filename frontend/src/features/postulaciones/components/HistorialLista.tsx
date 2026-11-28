import Box from '@mui/material/Box'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { formatearFechaHora } from '@/shared/lib/formatos'
import type { EstadoPostulacion, FechaISO } from '@/shared/api/types'

export interface ItemHistorial {
  estadoAnterior: EstadoPostulacion | null
  estadoNuevo: EstadoPostulacion
  fecha: FechaISO
  comentario: string | null
  /** Solo lo ve la empresa. */
  cambiadoPor?: string
}

/** Línea de tiempo de una postulación, de lo más reciente a lo más antiguo. */
export function HistorialLista({ items }: { items: ItemHistorial[] }) {
  if (items.length === 0) return <Typography color="text.secondary">Sin movimientos todavía.</Typography>

  const ordenados = [...items].sort((a, b) => b.fecha.localeCompare(a.fecha))

  return (
    <Stack component="ol" spacing={1.5} sx={{ listStyle: 'none', m: 0, p: 0 }} aria-label="Historial">
      {ordenados.map((item, indice) => (
        <Box
          component="li"
          key={`${item.fecha}-${indice}`}
          sx={{ borderLeft: 3, borderColor: 'divider', pl: 2 }}
        >
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            {item.estadoAnterior && (
              <>
                <ChipEstado estado={item.estadoAnterior} />
                <Typography component="span" aria-hidden>
                  →
                </Typography>
              </>
            )}
            <ChipEstado estado={item.estadoNuevo} />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {formatearFechaHora(item.fecha)}
            {item.cambiadoPor ? ` · ${item.cambiadoPor}` : ''}
          </Typography>
          {item.comentario && <Typography variant="body2">{item.comentario}</Typography>}
        </Box>
      ))}
    </Stack>
  )
}
