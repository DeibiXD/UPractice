import Chip, { type ChipProps } from '@mui/material/Chip'

type Color = ChipProps['color']

// Estados del contrato (docs/contratos/api-types.ts): vacante, postulación, habilitación y verificación.
const COLORES: Record<string, Color> = {
  Borrador: 'default',
  Publicada: 'success',
  Pausada: 'warning',
  Cerrada: 'default',
  Enviada: 'info',
  EnRevision: 'info',
  Entrevista: 'secondary',
  Oferta: 'warning',
  Aceptada: 'success',
  Declinada: 'default',
  Rechazada: 'error',
  Retirada: 'default',
  Pendiente: 'warning',
  Aprobada: 'success',
  Verificado: 'success',
  Rechazado: 'error',
}

const ETIQUETAS: Record<string, string> = {
  EnRevision: 'En revisión',
}

/** Etiqueta de color para cualquier estado del contrato. Un estado desconocido sale en gris. */
export function ChipEstado({ estado }: { estado: string }) {
  return <Chip size="small" label={ETIQUETAS[estado] ?? estado} color={COLORES[estado] ?? 'default'} />
}
