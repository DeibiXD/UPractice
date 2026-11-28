import Button from '@mui/material/Button'
import MenuItem from '@mui/material/MenuItem'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableContainer from '@mui/material/TableContainer'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useState } from 'react'
import { Link as EnlaceRouter } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import type { EstadoPostulacion } from '@/shared/api/types'
import { Cargando } from '@/shared/components/Cargando'
import { EstadoError } from '@/shared/components/EstadoError'
import { EstadoVacio } from '@/shared/components/EstadoVacio'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { ESTADOS_FILTRO, ETIQUETA_ESTADO, formatearFechaHora } from '../estados'
import { useMisPostulaciones } from '../hooks'

/** HU-06: mis postulaciones, con filtro por estado. */
export function MisPostulacionesPage() {
  const [estado, setEstado] = useState<EstadoPostulacion | ''>('')
  const consulta = useMisPostulaciones(estado || undefined)

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: { sm: 'center' } }}>
        <Typography variant="h5" component="h1" sx={{ flexGrow: 1 }}>
          Mis postulaciones
        </Typography>
        <TextField
          select
          size="small"
          label="Estado"
          value={estado}
          onChange={(evento) => setEstado(evento.target.value as EstadoPostulacion | '')}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">Todos</MenuItem>
          {ESTADOS_FILTRO.map((e) => (
            <MenuItem key={e} value={e}>
              {ETIQUETA_ESTADO[e]}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      {consulta.isPending && <Cargando />}
      {consulta.isError && (
        <EstadoError mensaje={extraerError(consulta.error).mensaje} onReintentar={() => void consulta.refetch()} />
      )}

      {consulta.isSuccess && consulta.data.length === 0 && (
        <EstadoVacio
          titulo={estado ? 'Sin resultados' : 'Aún no tienes postulaciones'}
          descripcion={
            estado ? 'No hay postulaciones con ese estado.' : 'Todavía no te has postulado a ninguna vacante.'
          }
        />
      )}

      {consulta.isSuccess && consulta.data.length > 0 && (
        <TableContainer component={Paper}>
          <Table aria-label="Mis postulaciones">
            <TableHead>
              <TableRow>
                <TableCell>Vacante</TableCell>
                <TableCell>Empresa</TableCell>
                <TableCell>Estado</TableCell>
                <TableCell>Postulada</TableCell>
                <TableCell>Último cambio</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {consulta.data.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>{p.vacante.titulo}</TableCell>
                  <TableCell>{p.empresa.nombre}</TableCell>
                  <TableCell>
                    <ChipEstado estado={p.estado} />
                  </TableCell>
                  <TableCell>{formatearFechaHora(p.fechaPostulacion)}</TableCell>
                  <TableCell>{formatearFechaHora(p.fechaUltimoCambio)}</TableCell>
                  <TableCell align="right">
                    <Button component={EnlaceRouter} to={`/estudiante/postulaciones/${p.id}`} size="small">
                      Ver detalle
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  )
}
