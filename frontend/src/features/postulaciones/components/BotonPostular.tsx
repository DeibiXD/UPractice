import Button from '@mui/material/Button'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Guid } from '@/shared/api/types'
import { PostularDialog } from './PostularDialog'

interface Props {
  vacanteId: Guid
  vacanteTitulo: string
  /** true si el estudiante ya tiene una postulación activa (VacanteResumenDto.yaPostulado). */
  yaPostulado?: boolean
}

/** Botón listo para el detalle de vacante: abre el diálogo y, al terminar, lleva al detalle de la postulación. */
export function BotonPostular({ vacanteId, vacanteTitulo, yaPostulado = false }: Props) {
  const [abierto, setAbierto] = useState(false)
  const navegar = useNavigate()

  return (
    <>
      <Button variant="contained" onClick={() => setAbierto(true)} disabled={yaPostulado}>
        {yaPostulado ? 'Ya te postulaste' : 'Postularme'}
      </Button>
      <PostularDialog
        vacanteId={vacanteId}
        vacanteTitulo={vacanteTitulo}
        abierto={abierto}
        onCerrar={() => setAbierto(false)}
        alExito={(postulacion) => navegar(`/estudiante/postulaciones/${postulacion.id}`)}
      />
    </>
  )
}
