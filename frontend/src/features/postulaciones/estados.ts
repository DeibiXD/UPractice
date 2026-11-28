import type { AccionPostulacion, EstadoPostulacion, FechaISO } from '@/shared/api/types'

/** Textos de los estados de una postulación (S3.6 del diseño); los colores los pone ChipEstado. */
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

/** Orden en que se ofrecen en el filtro del listado. */
export const ESTADOS_FILTRO: EstadoPostulacion[] = [
  'Enviada',
  'EnRevision',
  'Entrevista',
  'Oferta',
  'Aceptada',
  'Rechazada',
  'Declinada',
  'Retirada',
]

export const ETIQUETA_ACCION: Record<AccionPostulacion, string> = {
  Retirar: 'Retirar postulación',
  Reactivar: 'Reactivar postulación',
  AceptarOferta: 'Aceptar oferta',
  DeclinarOferta: 'Declinar oferta',
}

/** Las acciones se pintan siempre en este orden, sin importar cómo las envíe la API. */
const ORDEN_ACCIONES: AccionPostulacion[] = ['AceptarOferta', 'DeclinarOferta', 'Reactivar', 'Retirar']

export function ordenarAcciones(acciones: readonly AccionPostulacion[]): AccionPostulacion[] {
  return ORDEN_ACCIONES.filter((accion) => acciones.includes(accion))
}

const formatoFecha = new Intl.DateTimeFormat('es-HN', {
  timeZone: 'America/Tegucigalpa',
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

/** Fecha y hora en horario de Honduras; si el texto no es una fecha, devuelve un guion. */
export function formatearFechaHora(fecha: FechaISO | null | undefined): string {
  if (!fecha) return '—'
  const valor = new Date(fecha)
  return Number.isNaN(valor.getTime()) ? '—' : formatoFecha.format(valor)
}

/** Currículum que se usará: el elegido, si no el principal y, si no, el primero. null si no hay ninguno. */
export function curriculumEfectivo<T extends { id: string; esPrincipal: boolean }>(
  elegido: string | null,
  lista: readonly T[],
): string | null {
  if (elegido && lista.some((c) => c.id === elegido)) return elegido
  return (lista.find((c) => c.esPrincipal) ?? lista[0])?.id ?? null
}
