import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useState } from 'react'
import { extraerError, type ErrorApi } from '@/shared/api/errores'
import type { Guid, MiPostulacionDetalleDto } from '@/shared/api/types'
import { curriculumEfectivo } from '../estados'
import { useMisCurriculums, usePostular, useReactivar } from '../hooks'
import { SelectorCurriculum } from './SelectorCurriculum'

interface Props {
  abierto: boolean
  onCerrar: () => void
  titulo: string
  /** Nombre de la vacante, para el texto de confirmación. */
  vacanteTitulo: string
  textoAccion: string
  /** Hace la llamada con el currículum elegido. */
  enviar: (curriculumId: Guid) => Promise<MiPostulacionDetalleDto>
  alExito: (resultado: MiPostulacionDetalleDto) => void
}

/** Base común de "Postularme" (HU-05) y "Reactivar" (HU-06): ambas piden elegir currículum. */
function DialogoCurriculum({ abierto, onCerrar, titulo, vacanteTitulo, textoAccion, enviar, alExito }: Props) {
  const curriculums = useMisCurriculums()
  const [elegido, setElegido] = useState<Guid | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<ErrorApi | null>(null)

  const lista = curriculums.data ?? []
  const efectivo = curriculumEfectivo(elegido, lista)

  const cerrar = () => {
    if (enviando) return
    setError(null)
    setElegido(null)
    onCerrar()
  }

  const confirmar = async () => {
    if (!efectivo) return
    setError(null)
    setEnviando(true)
    try {
      alExito(await enviar(efectivo))
      setElegido(null)
    } catch (causa) {
      setError(extraerError(causa))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <Dialog open={abierto} onClose={cerrar} fullWidth maxWidth="sm">
      <DialogTitle>{titulo}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <Typography>{vacanteTitulo}</Typography>
          {error && <Alert severity="error">{error.mensaje}</Alert>}
          {curriculums.isPending && <CircularProgress size={24} aria-label="Cargando currículums" />}
          {curriculums.isError && <Alert severity="error">{extraerError(curriculums.error).mensaje}</Alert>}
          {curriculums.isSuccess && lista.length === 0 && (
            <Alert severity="warning">No tiene currículums. Suba uno antes de continuar.</Alert>
          )}
          {efectivo && (
            <SelectorCurriculum
              lista={lista}
              valor={efectivo}
              onChange={setElegido}
              error={error?.campos.curriculumId}
            />
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={cerrar} disabled={enviando}>
          Cancelar
        </Button>
        <Button variant="contained" onClick={confirmar} disabled={!efectivo || enviando}>
          {enviando ? 'Enviando…' : textoAccion}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

interface PostularProps {
  vacanteId: Guid
  vacanteTitulo: string
  abierto: boolean
  onCerrar: () => void
  alExito: (postulacion: MiPostulacionDetalleDto) => void
}

/** HU-05. La pantalla de detalle de vacante (HU-04) lo abre desde su botón "Postularme". */
export function PostularDialog({ vacanteId, vacanteTitulo, abierto, onCerrar, alExito }: PostularProps) {
  const postular = usePostular(vacanteId)
  return (
    <DialogoCurriculum
      abierto={abierto}
      onCerrar={onCerrar}
      titulo="Postularme a la vacante"
      vacanteTitulo={vacanteTitulo}
      textoAccion="Postularme"
      enviar={(curriculumId) => postular.mutateAsync({ curriculumId })}
      alExito={alExito}
    />
  )
}

interface ReactivarProps {
  postulacionId: Guid
  vacanteTitulo: string
  abierto: boolean
  onCerrar: () => void
}

/** HU-06: volver a enviar una postulación retirada, pudiendo elegir otro currículum. */
export function ReactivarDialog({ postulacionId, vacanteTitulo, abierto, onCerrar }: ReactivarProps) {
  const reactivar = useReactivar(postulacionId)
  return (
    <DialogoCurriculum
      abierto={abierto}
      onCerrar={onCerrar}
      titulo="Reactivar postulación"
      vacanteTitulo={vacanteTitulo}
      textoAccion="Reactivar"
      enviar={(curriculumId) => reactivar.mutateAsync({ curriculumId })}
      alExito={onCerrar}
    />
  )
}
