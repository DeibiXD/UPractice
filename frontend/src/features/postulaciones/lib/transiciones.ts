import type { AccionPostulacion, CambiarEstadoPostulacionRequest, EstadoPostulacion } from '@/shared/api/types'

export type EstadoDestinoEmpresa = CambiarEstadoPostulacionRequest['nuevoEstado']

/**
 * Espejo de backend/.../Domain/Reglas/TransicionesPostulacion.cs para el actor Empresa.
 * El servidor es la autoridad: si esta tabla se desactualiza, responde 422 TRANSICION_INVALIDA.
 */
const SIGUIENTES_EMPRESA: Partial<Record<EstadoPostulacion, EstadoDestinoEmpresa[]>> = {
  Enviada: ['EnRevision', 'Entrevista', 'Rechazada'],
  EnRevision: ['Entrevista', 'Oferta', 'Rechazada'],
  Entrevista: ['Oferta', 'Rechazada'],
}

/** Estados a los que la empresa puede mover una postulación desde `estado` (vacío si ya no puede). */
export function siguientesEmpresa(estado: EstadoPostulacion): EstadoDestinoEmpresa[] {
  return SIGUIENTES_EMPRESA[estado] ?? []
}

export const ESTADOS_POSTULACION: EstadoPostulacion[] = [
  'Enviada',
  'EnRevision',
  'Entrevista',
  'Oferta',
  'Aceptada',
  'Declinada',
  'Rechazada',
  'Retirada',
]

export const ETIQUETA_ESTADO: Record<EstadoPostulacion, string> = {
  Enviada: 'Enviada',
  EnRevision: 'En revisión',
  Entrevista: 'Entrevista',
  Oferta: 'Oferta',
  Aceptada: 'Aceptada',
  Declinada: 'Declinada',
  Rechazada: 'Rechazada',
  Retirada: 'Retirada',
}

export const ETIQUETA_ACCION: Record<AccionPostulacion, string> = {
  Retirar: 'Retirar postulación',
  Reactivar: 'Reactivar postulación',
  AceptarOferta: 'Aceptar oferta',
  DeclinarOferta: 'Declinar oferta',
}

export function etiquetaModalidad(modalidad: string): string {
  return modalidad === 'Hibrida' ? 'Híbrida' : modalidad
}
