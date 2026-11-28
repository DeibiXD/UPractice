import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import type { FiltroVacantesQuery, Modalidad, VacanteResumenDto } from '@/shared/api/types'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { Paginacion } from '@/shared/components/Paginacion'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { formatearFecha } from '@/shared/lib/formatos'
import { useVacantes } from '../../hooks'
import { etiquetaModalidad } from '../../lib/transiciones'

const TAMANO_PAGINA = 10

const columnas: ColumnaTabla<VacanteResumenDto>[] = [
  { id: 'titulo', encabezado: 'Vacante', render: (v) => v.titulo },
  { id: 'empresa', encabezado: 'Empresa', render: (v) => v.empresa.nombre },
  { id: 'modalidad', encabezado: 'Modalidad', render: (v) => etiquetaModalidad(v.modalidad) },
  { id: 'ubicacion', encabezado: 'Ubicación', render: (v) => v.ubicacion },
  { id: 'remunerada', encabezado: 'Remunerada', render: (v) => (v.remunerada ? 'Sí' : 'No') },
  { id: 'cierre', encabezado: 'Cierra', render: (v) => formatearFecha(v.fechaCierre) },
  {
    id: 'estado',
    encabezado: 'Su perfil',
    render: (v) => (
      <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', rowGap: 0.5 }}>
        {v.yaPostulado && <Chip size="small" color="info" label="Postulado" />}
        <Chip
          size="small"
          color={v.cumploRequisitos ? 'success' : 'default'}
          label={v.cumploRequisitos ? 'Cumple requisitos' : 'Requisitos parciales'}
        />
      </Stack>
    ),
  },
]

/**
 * Explorar vacantes (estudiante). Pantalla provisional de HU-04 para que el flujo de postulación
 * pueda probarse de punta a punta; si HU-04 entrega la suya, reemplaza esta y conserva <BotonPostular>.
 */
export function VacantesPage() {
  const navigate = useNavigate()
  const [filtro, setFiltro] = useState<FiltroVacantesQuery>({ pagina: 1, tamanoPagina: TAMANO_PAGINA })
  const [busqueda, setBusqueda] = useState('')
  const [modalidad, setModalidad] = useState('')
  const [remunerada, setRemunerada] = useState('')
  const consulta = useVacantes(filtro)

  const buscar = (evento: FormEvent) => {
    evento.preventDefault()
    setFiltro({
      busqueda: busqueda.trim() || undefined,
      modalidad: (modalidad || undefined) as Modalidad | undefined,
      remunerada: remunerada === '' ? undefined : remunerada === 'si',
      pagina: 1,
      tamanoPagina: TAMANO_PAGINA,
    })
  }

  return (
    <>
      <EncabezadoPagina titulo="Vacantes" subtitulo="Vacantes publicadas por empresas habilitadas en su universidad." />

      <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
        <Stack
          component="form"
          direction={{ xs: 'column', md: 'row' }}
          spacing={2}
          onSubmit={buscar}
          noValidate
          role="search"
        >
          <TextField
            label="Buscar por título o empresa"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            sx={{ flexGrow: 1 }}
          />
          <TextField
            select
            label="Modalidad"
            value={modalidad}
            onChange={(e) => setModalidad(e.target.value)}
            slotProps={{ select: { native: true } }}
            sx={{ minWidth: 160 }}
          >
            <option value="">Todas</option>
            <option value="Presencial">Presencial</option>
            <option value="Remota">Remota</option>
            <option value="Hibrida">Híbrida</option>
          </TextField>
          <TextField
            select
            label="Remuneración"
            value={remunerada}
            onChange={(e) => setRemunerada(e.target.value)}
            slotProps={{ select: { native: true } }}
            sx={{ minWidth: 160 }}
          >
            <option value="">Todas</option>
            <option value="si">Remuneradas</option>
            <option value="no">No remuneradas</option>
          </TextField>
          <Button type="submit" variant="contained">
            Buscar
          </Button>
        </Stack>
      </Paper>

      <TablaDatos
        etiqueta="Vacantes disponibles"
        columnas={columnas}
        filas={consulta.data?.items}
        idFila={(v) => v.id}
        cargando={consulta.isPending}
        error={consulta.isError ? extraerError(consulta.error).mensaje : null}
        onReintentar={() => void consulta.refetch()}
        vacio={{ titulo: 'No hay vacantes disponibles con esos filtros.' }}
        onFilaClick={(v) => void navigate(`/estudiante/vacantes/${v.id}`)}
      />
      <Paginacion
        pagina={consulta.data?.pagina ?? 1}
        totalPaginas={consulta.data?.totalPaginas ?? 1}
        onCambiar={(pagina) => setFiltro((actual) => ({ ...actual, pagina }))}
      />
    </>
  )
}
