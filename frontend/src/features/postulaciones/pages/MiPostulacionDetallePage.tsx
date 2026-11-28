import Button from '@mui/material/Button'
import Divider from '@mui/material/Divider'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Link as EnlaceRouter, useParams } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import { Cargando } from '@/shared/components/Cargando'
import { EstadoError } from '@/shared/components/EstadoError'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { AccionesPostulacion } from '../components/AccionesPostulacion'
import { ETIQUETA_ESTADO, formatearFechaHora } from '../estados'
import { useMiPostulacion } from '../hooks'

function Dato({ titulo, valor }: { titulo: string; valor: string }) {
  return (
    <Stack>
      <Typography variant="caption" color="text.secondary">
        {titulo}
      </Typography>
      <Typography>{valor}</Typography>
    </Stack>
  )
}

/** HU-06: detalle de una postulación propia, con sus acciones e historial. */
export function MiPostulacionDetallePage() {
  const { id = '' } = useParams()
  const consulta = useMiPostulacion(id)

  const volver = (
    <Button component={EnlaceRouter} to="/estudiante/postulaciones" sx={{ alignSelf: 'flex-start' }}>
      ← Mis postulaciones
    </Button>
  )

  if (consulta.isPending) return <Cargando />
  if (consulta.isError) {
    return (
      <Stack spacing={2}>
        {volver}
        <EstadoError mensaje={extraerError(consulta.error).mensaje} onReintentar={() => void consulta.refetch()} />
      </Stack>
    )
  }

  const p = consulta.data
  // Del más reciente al más antiguo.
  const historial = [...p.historial].sort((a, b) => b.fecha.localeCompare(a.fecha))

  return (
    <Stack spacing={2}>
      {volver}
      <Paper sx={{ p: 3 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <Typography variant="h5" component="h1" sx={{ flexGrow: 1 }}>
              {p.vacante.titulo}
            </Typography>
            <ChipEstado estado={p.estado} />
          </Stack>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={{ xs: 1, sm: 4 }}>
            <Dato titulo="Empresa" valor={p.empresa.nombre} />
            <Dato titulo="Currículum" valor={p.curriculum.nombreVersion} />
            <Dato titulo="Postulada" valor={formatearFechaHora(p.fechaPostulacion)} />
            <Dato titulo="Último cambio" valor={formatearFechaHora(p.fechaUltimoCambio)} />
          </Stack>

          <AccionesPostulacion postulacion={p} />
        </Stack>
      </Paper>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" component="h2" gutterBottom>
          Historial
        </Typography>
        {historial.length === 0 ? (
          <Typography color="text.secondary">Sin movimientos.</Typography>
        ) : (
          <Stack divider={<Divider flexItem />} spacing={1.5} component="ol" sx={{ m: 0, p: 0, listStyle: 'none' }}>
            {historial.map((h) => (
              <Stack component="li" key={`${h.fecha}-${h.estadoNuevo}`} spacing={0.5}>
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                  <Typography variant="body2" color="text.secondary">
                    {h.estadoAnterior ? ETIQUETA_ESTADO[h.estadoAnterior] : 'Inicio'} →
                  </Typography>
                  <ChipEstado estado={h.estadoNuevo} />
                  <Typography variant="caption" color="text.secondary">
                    {formatearFechaHora(h.fecha)}
                  </Typography>
                </Stack>
                {h.comentario && <Typography variant="body2">{h.comentario}</Typography>}
              </Stack>
            ))}
          </Stack>
        )}
      </Paper>
    </Stack>
  )
}
