import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import type { EstadoPostulacion, MiPostulacionDto } from '@/shared/api/types'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { formatearFecha } from '@/shared/lib/formatos'
import { useMisPostulaciones } from '../../hooks'
import { ESTADOS_POSTULACION, ETIQUETA_ESTADO } from '../../lib/transiciones'

const columnas: ColumnaTabla<MiPostulacionDto>[] = [
  { id: 'vacante', encabezado: 'Vacante', render: (p) => p.vacante.titulo },
  { id: 'empresa', encabezado: 'Empresa', render: (p) => p.empresa.nombre },
  { id: 'estado', encabezado: 'Estado', render: (p) => <ChipEstado estado={p.estado} /> },
  { id: 'fecha', encabezado: 'Postulación', render: (p) => formatearFecha(p.fechaPostulacion) },
  { id: 'cambio', encabezado: 'Último cambio', render: (p) => formatearFecha(p.fechaUltimoCambio) },
]

/** HU-06: seguimiento de las postulaciones del estudiante. */
export function MisPostulacionesPage() {
  const navigate = useNavigate()
  const [estado, setEstado] = useState<EstadoPostulacion | ''>('')
  const consulta = useMisPostulaciones(estado || undefined)

  return (
    <>
      <EncabezadoPagina titulo="Mis postulaciones" subtitulo="Siga el estado de cada postulación y responda a las ofertas." />

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <TextField
          select
          label="Estado"
          value={estado}
          onChange={(e) => setEstado(e.target.value as EstadoPostulacion | '')}
          slotProps={{ select: { native: true } }}
          sx={{ minWidth: 220 }}
        >
          <option value="">Todos</option>
          {ESTADOS_POSTULACION.map((e) => (
            <option key={e} value={e}>
              {ETIQUETA_ESTADO[e]}
            </option>
          ))}
        </TextField>
      </Paper>

      <TablaDatos
        etiqueta="Mis postulaciones"
        columnas={columnas}
        filas={consulta.data}
        idFila={(p) => p.id}
        cargando={consulta.isPending}
        error={consulta.isError ? extraerError(consulta.error).mensaje : null}
        onReintentar={() => void consulta.refetch()}
        vacio={{
          titulo: estado ? 'No tiene postulaciones en ese estado.' : 'Aún no tiene postulaciones.',
          descripcion: estado ? undefined : 'Explore las vacantes y postúlese a las que le interesen.',
          accion: estado ? undefined : (
            <Button component={Link} to="/estudiante/vacantes" variant="contained">
              Explorar vacantes
            </Button>
          ),
        }}
        onFilaClick={(p) => void navigate(`/estudiante/postulaciones/${p.id}`)}
      />
    </>
  )
}
