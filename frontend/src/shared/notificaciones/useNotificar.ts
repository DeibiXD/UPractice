import { useContext } from 'react'
import { NotificacionesContext } from '@/shared/notificaciones/contexto'

/** const notificar = useNotificar();  notificar.exito('Vacante publicada.'); */
export function useNotificar() {
  const contexto = useContext(NotificacionesContext)
  if (!contexto) throw new Error('useNotificar debe usarse dentro de <NotificacionesProvider>')
  return contexto
}
