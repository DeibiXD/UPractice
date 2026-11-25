import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import type { ReactNode } from 'react'

interface Props {
  abierto: boolean
  titulo: string
  mensaje: ReactNode
  textoConfirmar?: string
  textoCancelar?: string
  /** Acción destructiva (cerrar vacante, rechazar, eliminar): el botón sale en rojo. */
  peligro?: boolean
  /** Mientras la acción se ejecuta, se bloquean ambos botones. */
  cargando?: boolean
  onConfirmar: () => void
  onCancelar: () => void
}

export function ConfirmDialog({
  abierto,
  titulo,
  mensaje,
  textoConfirmar = 'Confirmar',
  textoCancelar = 'Cancelar',
  peligro = false,
  cargando = false,
  onConfirmar,
  onCancelar,
}: Props) {
  return (
    <Dialog open={abierto} onClose={cargando ? undefined : onCancelar} maxWidth="xs" fullWidth>
      <DialogTitle>{titulo}</DialogTitle>
      <DialogContent>
        <DialogContentText component="div">{mensaje}</DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancelar} disabled={cargando}>
          {textoCancelar}
        </Button>
        <Button
          onClick={onConfirmar}
          variant="contained"
          color={peligro ? 'error' : 'primary'}
          disabled={cargando}
        >
          {cargando ? 'Procesando…' : textoConfirmar}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
