import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import { Cargando } from '@/shared/components/Cargando'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { EstadoError } from '@/shared/components/EstadoError'
import { formatearFecha } from '@/shared/lib/formatos'
import { BotonPostular } from '../../components/BotonPostular'
import { useVacante } from '../../hooks'
import { etiquetaModalidad } from '../../lib/transiciones'

function Dato({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <Box>
      <Typography variant="overline" color="text.secondary">
        {etiqueta}
      </Typography>
      <Typography component="div">{children}</Typography>
    </Box>
  )
}

/** Detalle de una vacante con el botón Postular (HU-05). Provisional de HU-04, ver VacantesPage. */
export function VacanteDetallePage() {
  const { id = '' } = useParams()
  const consulta = useVacante(id)

  if (consulta.isPending) return <Cargando />
  if (consulta.isError) {
    return <EstadoError mensaje={extraerError(consulta.error).mensaje} onReintentar={() => void consulta.refetch()} />
  }

  const vacante = consulta.data

  return (
    <>
      <Button component={Link} to="/estudiante/vacantes" size="small" sx={{ mb: 1 }}>
        ← Vacantes
      </Button>

      <EncabezadoPagina
        titulo={vacante.titulo}
        subtitulo={vacante.empresa.nombre}
        acciones={
          <BotonPostular vacanteId={vacante.id} vacanteTitulo={vacante.titulo} yaPostulado={vacante.yaPostulado} />
        }
      />

      {!vacante.cumploRequisitos && !vacante.yaPostulado && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Su perfil no cumple todos los requisitos deseables de esta vacante. Aun así puede postularse: la empresa
          verá qué requisitos cumple.
        </Alert>
      )}

      <Paper variant="outlined" sx={{ p: 3 }}>
        <Box
          sx={{
            display: 'grid',
            gap: 2,
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' },
            mb: 3,
          }}
        >
          <Dato etiqueta="Modalidad">{etiquetaModalidad(vacante.modalidad)}</Dato>
          <Dato etiqueta="Ubicación">{vacante.ubicacion}</Dato>
          <Dato etiqueta="Remunerada">{vacante.remunerada ? 'Sí' : 'No'}</Dato>
          <Dato etiqueta="Cupos">{vacante.cupos}</Dato>
          <Dato etiqueta="Cierra">{formatearFecha(vacante.fechaCierre)}</Dato>
          <Dato etiqueta="Índice mínimo deseable">{vacante.indiceMinimo ?? 'No requerido'}</Dato>
        </Box>

        <Typography variant="overline" color="text.secondary">
          Carreras
        </Typography>
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', rowGap: 1, mb: 3 }}>
          {vacante.carreras.map((carrera) => (
            <Chip key={carrera} label={carrera} size="small" />
          ))}
        </Stack>

        {vacante.habilidades.length > 0 && (
          <>
            <Typography variant="overline" color="text.secondary">
              Habilidades
            </Typography>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', rowGap: 1, mb: 3 }}>
              {vacante.habilidades.map((habilidad) => (
                <Chip
                  key={habilidad.id}
                  size="small"
                  color={habilidad.esRequerida ? 'primary' : 'default'}
                  variant={habilidad.esRequerida ? 'filled' : 'outlined'}
                  label={habilidad.esRequerida ? `${habilidad.nombre} (requerida)` : habilidad.nombre}
                />
              ))}
            </Stack>
          </>
        )}

        <Typography variant="overline" color="text.secondary">
          Descripción
        </Typography>
        <Typography sx={{ whiteSpace: 'pre-wrap' }}>{vacante.descripcion}</Typography>
      </Paper>
    </>
  )
}
