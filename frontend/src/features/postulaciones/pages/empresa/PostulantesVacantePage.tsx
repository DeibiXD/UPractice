import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import type { EstadoPostulacion, FiltroPostulantesQuery, PostulanteResumenDto } from '@/shared/api/types'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { formatearFecha } from '@/shared/lib/formatos'
import { usePostulantes, useVacanteEmpresa } from '../../hooks'
import { ESTADOS_POSTULACION, ETIQUETA_ESTADO } from '../../lib/transiciones'

const columnas: ColumnaTabla<PostulanteResumenDto>[] = [
  { id: 'nombre', encabezado: 'Estudiante', render: (p) => p.estudiante.nombreCompleto },
  { id: 'universidad', encabezado: 'Universidad', render: (p) => p.estudiante.universidad },
  { id: 'carrera', encabezado: 'Carrera', render: (p) => p.estudiante.carrera },
  { id: 'estado', encabezado: 'Estado', render: (p) => <ChipEstado estado={p.estado} /> },
  {
    id: 'requisitos',
    encabezado: 'Requisitos',
    render: (p) => (
      <Chip
        size="small"
        color={p.cumpleRequisitos ? 'success' : 'default'}
        label={p.cumpleRequisitos ? 'Cumple' : 'Parcial'}
      />
    ),
  },
  { id: 'fecha', encabezado: 'Postulación', render: (p) => formatearFecha(p.fechaPostulacion) },
]

/** HU-E05: postulantes de una vacante, con filtros por estado y por cumplimiento de requisitos. */
export function PostulantesVacantePage() {
  const { vacanteId = '' } = useParams()
  const navigate = useNavigate()
  const [estado, setEstado] = useState<EstadoPostulacion | ''>('')
  const [requisitos, setRequisitos] = useState<'' | 'si' | 'no'>('')

  const filtro: FiltroPostulantesQuery = {
    estado: estado || undefined,
    cumpleRequisitos: requisitos === '' ? undefined : requisitos === 'si',
  }
  const consulta = usePostulantes(vacanteId, filtro)
  const vacante = useVacanteEmpresa(vacanteId)
  const hayFiltros = estado !== '' || requisitos !== ''

  return (
    <>
      <Button component={Link} to="/empresa/postulantes" size="small" sx={{ mb: 1 }}>
        ← Postulantes
      </Button>

      <EncabezadoPagina
        titulo={vacante.data ? `Postulantes de «${vacante.data.titulo}»` : 'Postulantes de la vacante'}
        subtitulo={vacante.data ? `${vacante.data.totalPostulaciones} postulación(es) en total.` : undefined}
      />

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <TextField
            select
            label="Estado"
            value={estado}
            onChange={(e) => setEstado(e.target.value as EstadoPostulacion | '')}
            slotProps={{ select: { native: true } }}
            sx={{ minWidth: 200 }}
          >
            <option value="">Todos</option>
            {ESTADOS_POSTULACION.map((e) => (
              <option key={e} value={e}>
                {ETIQUETA_ESTADO[e]}
              </option>
            ))}
          </TextField>
          <TextField
            select
            label="Requisitos"
            value={requisitos}
            onChange={(e) => setRequisitos(e.target.value as '' | 'si' | 'no')}
            slotProps={{ select: { native: true } }}
            sx={{ minWidth: 200 }}
          >
            <option value="">Todos</option>
            <option value="si">Cumplen requisitos</option>
            <option value="no">No cumplen todos</option>
          </TextField>
        </Stack>
      </Paper>

      <TablaDatos
        etiqueta="Postulantes de la vacante"
        columnas={columnas}
        filas={consulta.data}
        idFila={(p) => p.postulacionId}
        cargando={consulta.isPending}
        error={consulta.isError ? extraerError(consulta.error).mensaje : null}
        onReintentar={() => void consulta.refetch()}
        vacio={{
          titulo: hayFiltros ? 'Ningún postulante coincide con los filtros.' : 'Aún no hay postulantes en esta vacante.',
        }}
        onFilaClick={(p) => void navigate(`/empresa/postulaciones/${p.postulacionId}`)}
      />
    </>
  )
}
