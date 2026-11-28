import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { usePostular } from '../hooks'
import { SeleccionarCurriculumDialog } from './SeleccionarCurriculumDialog'

interface Props {
  vacanteId: string
  vacanteTitulo: string
  /** `yaPostulado` del contrato: cualquier postulación propia, incluso Retirada. */
  yaPostulado: boolean
}

/**
 * Botón "Postular" con selección de currículum. Se exporta desde el index de la feature para que
 * cualquier pantalla de vacantes lo reutilice: <BotonPostular vacanteId=… vacanteTitulo=… yaPostulado=… />.
 */
export function BotonPostular({ vacanteId, vacanteTitulo, yaPostulado }: Props) {
  const [abierto, setAbierto] = useState(false)
  const postular = usePostular(vacanteId)
  const notificar = useNotificar()

  if (yaPostulado) {
    return (
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Chip label="Ya se postuló" color="info" size="small" />
        <Button component={Link} to="/estudiante/postulaciones" size="small">
          Ver mis postulaciones
        </Button>
      </Stack>
    )
  }

  return (
    <>
      <Button variant="contained" onClick={() => setAbierto(true)}>
        Postular
      </Button>
      <SeleccionarCurriculumDialog
        abierto={abierto}
        titulo={`Postularse a «${vacanteTitulo}»`}
        descripcion="Elija el currículum que verá la empresa. La postulación queda como “Enviada”."
        textoConfirmar="Enviar postulación"
        enviar={(curriculumId) => postular.mutateAsync(curriculumId)}
        onCerrar={() => setAbierto(false)}
        onExito={() => notificar.exito('Postulación enviada.')}
      />
    </>
  )
}
