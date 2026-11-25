import type { FechaISO, FechaSolo } from '@/shared/api/types'

const ZONA_HONDURAS = 'America/Tegucigalpa'
const SOLO_FECHA = /^\d{4}-\d{2}-\d{2}$/

type Valor = FechaISO | FechaSolo | null | undefined

/**
 * Fecha legible en español ("15 dic 2026").
 * Una FechaSolo ("2026-12-15") no tiene hora, así que NO se desplaza a la zona horaria
 * (en UTC-6 saldría un día antes). Una FechaISO se muestra en hora de Honduras.
 */
export function formatearFecha(valor: Valor): string {
  if (!valor) return '—'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return '—'
  const zona = SOLO_FECHA.test(valor) ? 'UTC' : ZONA_HONDURAS
  return new Intl.DateTimeFormat('es-HN', { dateStyle: 'medium', timeZone: zona }).format(fecha)
}

/** Fecha y hora en hora de Honduras ("15 dic 2026, 3:30 p. m."). */
export function formatearFechaHora(valor: FechaISO | null | undefined): string {
  if (!valor) return '—'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return '—'
  return new Intl.DateTimeFormat('es-HN', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: ZONA_HONDURAS,
  }).format(fecha)
}
