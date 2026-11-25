import { createContext } from 'react'

export interface Notificador {
  exito: (mensaje: string) => void
  error: (mensaje: string) => void
  info: (mensaje: string) => void
}

export const NotificacionesContext = createContext<Notificador | null>(null)
