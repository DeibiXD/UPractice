import Alert, { type AlertColor } from '@mui/material/Alert'
import Snackbar from '@mui/material/Snackbar'
import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { NotificacionesContext, type Notificador } from '@/shared/notificaciones/contexto'

interface Mensaje {
  clave: number
  severidad: AlertColor
  texto: string
}

export function NotificacionesProvider({ children }: { children: ReactNode }) {
  const [mensaje, setMensaje] = useState<Mensaje | null>(null)
  const [abierta, setAbierta] = useState(false)

  const mostrar = useCallback((severidad: AlertColor, texto: string) => {
    setMensaje({ clave: Date.now(), severidad, texto })
    setAbierta(true)
  }, [])

  const notificador = useMemo<Notificador>(
    () => ({
      exito: (texto) => mostrar('success', texto),
      error: (texto) => mostrar('error', texto),
      info: (texto) => mostrar('info', texto),
    }),
    [mostrar],
  )

  return (
    <NotificacionesContext.Provider value={notificador}>
      {children}
      <Snackbar
        key={mensaje?.clave}
        open={abierta}
        autoHideDuration={6000}
        onClose={(_evento, motivo) => {
          if (motivo !== 'clickaway') setAbierta(false)
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity={mensaje?.severidad ?? 'info'} variant="filled" onClose={() => setAbierta(false)}>
          {mensaje?.texto}
        </Alert>
      </Snackbar>
    </NotificacionesContext.Provider>
  )
}
