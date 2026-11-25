import Paper from '@mui/material/Paper'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import type { ReactNode } from 'react'
import { Cargando } from '@/shared/components/Cargando'
import { EstadoError } from '@/shared/components/EstadoError'
import { EstadoVacio } from '@/shared/components/EstadoVacio'

export interface ColumnaTabla<T> {
  id: string
  encabezado: string
  render: (fila: T) => ReactNode
  alinear?: 'left' | 'center' | 'right'
}

interface Props<T> {
  columnas: ColumnaTabla<T>[]
  /** undefined mientras no hay datos. */
  filas: T[] | undefined
  idFila: (fila: T) => string
  cargando?: boolean
  error?: string | null
  onReintentar?: () => void
  /** Mensaje cuando la lista llega vacía. */
  vacio?: { titulo: string; descripcion?: string; accion?: ReactNode }
  /** Si se define, la fila completa es clicable (también con teclado). */
  onFilaClick?: (fila: T) => void
  etiqueta?: string
}

/** Tabla con sus cuatro estados resueltos: cargando, error, vacía y con datos. */
export function TablaDatos<T>({
  columnas,
  filas,
  idFila,
  cargando = false,
  error = null,
  onReintentar,
  vacio = { titulo: 'No hay resultados.' },
  onFilaClick,
  etiqueta,
}: Props<T>) {
  if (cargando) return <Cargando />
  if (error) return <EstadoError mensaje={error} onReintentar={onReintentar} />
  if (!filas || filas.length === 0) return <EstadoVacio {...vacio} />

  return (
    <TableContainer component={Paper} variant="outlined">
      <Table size="small" aria-label={etiqueta}>
        <TableHead>
          <TableRow>
            {columnas.map((columna) => (
              <TableCell key={columna.id} align={columna.alinear} sx={{ fontWeight: 600 }}>
                {columna.encabezado}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {filas.map((fila) => (
            <TableRow
              key={idFila(fila)}
              hover={Boolean(onFilaClick)}
              tabIndex={onFilaClick ? 0 : undefined}
              onClick={onFilaClick ? () => onFilaClick(fila) : undefined}
              onKeyDown={
                onFilaClick
                  ? (evento) => {
                      if (evento.key === 'Enter') onFilaClick(fila)
                    }
                  : undefined
              }
              sx={onFilaClick ? { cursor: 'pointer' } : undefined}
            >
              {columnas.map((columna) => (
                <TableCell key={columna.id} align={columna.alinear}>
                  {columna.render(fila)}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}
