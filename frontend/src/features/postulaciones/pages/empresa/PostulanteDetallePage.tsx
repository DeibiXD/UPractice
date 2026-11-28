import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { Cargando } from '@/shared/components/Cargando'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { EstadoError } from '@/shared/components/EstadoError'
import { formatearFecha, formatearFechaHora } from '@/shared/lib/formatos'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { postulacionesApi } from '../../api'
import { CambiarEstadoDialog } from '../../components/CambiarEstadoDialog'
import { HistorialLista } from '../../components/HistorialLista'
import { useCambiarEstado, usePostulante } from '../../hooks'
import { siguientesEmpresa } from '../../lib/transiciones'

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

/** HU-E06 (perfil y CV del postulante) y HU-E07 (cambio de estado). */
export function PostulanteDetallePage() {
  const { id = '' } = useParams()
  const consulta = usePostulante(id)
  const cambiarEstado = useCambiarEstado(id)
  const notificar = useNotificar()
  const [dialogoAbierto, setDialogoAbierto] = useState(false)
  const [abriendoCv, setAbriendoCv] = useState(false)

  if (consulta.isPending) return <Cargando />
  if (consulta.isError) {
    return <EstadoError mensaje={extraerError(consulta.error).mensaje} onReintentar={() => void consulta.refetch()} />
  }

  const postulante = consulta.data
  const estudiante = postulante.estudiante
  const hayCambiosDisponibles = siguientesEmpresa(postulante.estado).length > 0

  const verCurriculum = async () => {
    setAbriendoCv(true)
    try {
      const { url } = await postulacionesApi.urlCurriculum(id)
      const ventana = window.open(url, '_blank', 'noopener,noreferrer')
      if (!ventana) notificar.error('El navegador bloqueó la ventana. Permita las ventanas emergentes e intente de nuevo.')
    } catch (causa) {
      notificar.error(extraerError(causa).mensaje)
    } finally {
      setAbriendoCv(false)
    }
  }

  return (
    <>
      <Button component={Link} to={`/empresa/vacantes/${postulante.vacante.id}/postulantes`} size="small" sx={{ mb: 1 }}>
        ← Postulantes de la vacante
      </Button>

      <EncabezadoPagina
        titulo={estudiante.nombreCompleto}
        subtitulo={`Postulación a «${postulante.vacante.titulo}»`}
        acciones={
          <>
            <Button variant="outlined" onClick={() => void verCurriculum()} disabled={abriendoCv}>
              {abriendoCv ? 'Abriendo…' : 'Ver currículum'}
            </Button>
            <Button variant="contained" onClick={() => setDialogoAbierto(true)} disabled={!hayCambiosDisponibles}>
              Cambiar estado
            </Button>
          </>
        }
      />

      <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 2, flexWrap: 'wrap', rowGap: 1 }}>
          <ChipEstado estado={postulante.estado} />
          <Chip
            size="small"
            color={postulante.cumpleRequisitos ? 'success' : 'default'}
            label={postulante.cumpleRequisitos ? 'Cumple requisitos' : 'Requisitos parciales'}
          />
          {!hayCambiosDisponibles && (
            <Typography variant="body2" color="text.secondary">
              No hay cambios de estado disponibles desde aquí.
            </Typography>
          )}
        </Stack>

        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' }, mb: 3 }}>
          <Dato etiqueta="Correo">{estudiante.correo}</Dato>
          <Dato etiqueta="Universidad">{estudiante.universidad}</Dato>
          <Dato etiqueta="Facultad">{estudiante.facultad}</Dato>
          <Dato etiqueta="Carrera">{estudiante.carrera}</Dato>
          <Dato etiqueta="Índice académico">{estudiante.indiceAcademico ?? 'No autorizado por el estudiante'}</Dato>
          <Dato etiqueta="Perfil verificado por">
            {estudiante.verificadoPor} · {formatearFecha(estudiante.fechaVerificacion)}
          </Dato>
          <Dato etiqueta="Postulación">{formatearFechaHora(postulante.fechaPostulacion)}</Dato>
          <Dato etiqueta="Currículum adjunto">{postulante.curriculum.nombreVersion}</Dato>
        </Box>

        <Typography variant="overline" color="text.secondary">
          Habilidades
        </Typography>
        {estudiante.habilidades.length === 0 ? (
          <Typography color="text.secondary">Sin habilidades registradas.</Typography>
        ) : (
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', rowGap: 1 }}>
            {estudiante.habilidades.map((habilidad) => (
              <Chip key={habilidad} label={habilidad} size="small" variant="outlined" />
            ))}
          </Stack>
        )}
      </Paper>

      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography variant="h6" component="h2" sx={{ mb: 2 }}>
          Historial
        </Typography>
        <HistorialLista items={postulante.historial} />
      </Paper>

      <CambiarEstadoDialog
        abierto={dialogoAbierto}
        estadoActual={postulante.estado}
        nombreEstudiante={estudiante.nombreCompleto}
        enviar={(datos) => cambiarEstado.mutateAsync(datos)}
        onCerrar={() => setDialogoAbierto(false)}
        onExito={() => notificar.exito('Estado actualizado.')}
      />
    </>
  )
}
