import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import type { VacanteEmpresaDto } from '@/shared/api/types'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { Paginacion } from '@/shared/components/Paginacion'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { formatearFecha } from '@/shared/lib/formatos'
import { useVacantesEmpresa } from '../../hooks'
import { ESTADOS_POSTULACION, ETIQUETA_ESTADO } from '../../lib/transiciones'

const columnas: ColumnaTabla<VacanteEmpresaDto>[] = [
  { id: 'titulo', encabezado: 'Vacante', render: (v) => v.titulo },
  { id: 'estado', encabezado: 'Estado', render: (v) => <ChipEstado estado={v.estado} /> },
  { id: 'cierre', encabezado: 'Cierra', render: (v) => formatearFecha(v.fechaCierre) },
  { id: 'total', encabezado: 'Postulaciones', alinear: 'right', render: (v) => v.totalPostulaciones },
  {
    id: 'desglose',
    encabezado: 'Por estado',
    render: (v) => (
      <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', rowGap: 0.5 }}>
        {ESTADOS_POSTULACION.filter((e) => (v.postulacionesPorEstado[e] ?? 0) > 0).map((e) => (
          <Chip key={e} size="small" variant="outlined" label={`${ETIQUETA_ESTADO[e]}: ${v.postulacionesPorEstado[e]}`} />
        ))}
      </Stack>
    ),
  },
  {
    id: 'accion',
    encabezado: '',
    alinear: 'right',
    render: (v) => (
      <Button component={Link} to={`/empresa/vacantes/${v.id}/postulantes`} size="small" variant="outlined">
        Ver postulantes
      </Button>
    ),
  },
]

/** HU-E05: punto de entrada a los postulantes. Lista las vacantes de la empresa con sus conteos. */
export function PostulantesPage() {
  const [pagina, setPagina] = useState(1)
  const consulta = useVacantesEmpresa(pagina)

  return (
    <>
      <EncabezadoPagina titulo="Postulantes" subtitulo="Elija una vacante para revisar a quienes se postularon." />
      <TablaDatos
        etiqueta="Vacantes de la empresa"
        columnas={columnas}
        filas={consulta.data?.items}
        idFila={(v) => v.id}
        cargando={consulta.isPending}
        error={consulta.isError ? extraerError(consulta.error).mensaje : null}
        onReintentar={() => void consulta.refetch()}
        vacio={{ titulo: 'Aún no hay vacantes.', descripcion: 'Cuando publique una vacante, sus postulantes aparecerán aquí.' }}
      />
      <Paginacion
        pagina={consulta.data?.pagina ?? 1}
        totalPaginas={consulta.data?.totalPaginas ?? 1}
        onCambiar={setPagina}
      />
    </>
  )
}
