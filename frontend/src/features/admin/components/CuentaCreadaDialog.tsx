import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import IconButton from '@mui/material/IconButton'
import Stack from '@mui/material/Stack'
import Tooltip from '@mui/material/Tooltip'
import Typography from '@mui/material/Typography'
import { useState } from 'react'
import type { CuentaCreadaDto } from '@/shared/api/types'

interface Props {
  cuenta: CuentaCreadaDto | null
  titulo: string
  onCerrar: () => void
}

/** Muestra la contraseña temporal UNA sola vez (D-17): no se puede volver a consultar. */
export function CuentaCreadaDialog({ cuenta, titulo, onCerrar }: Props) {
  const [copiado, setCopiado] = useState(false)

  const copiar = async () => {
    if (!cuenta) return
    await navigator.clipboard.writeText(cuenta.passwordTemporal)
    setCopiado(true)
  }

  return (
    <Dialog
      open={cuenta !== null}
      onClose={() => {
        setCopiado(false)
        onCerrar()
      }}
      fullWidth
      maxWidth="sm"
    >
      <DialogTitle>{titulo}</DialogTitle>
      <DialogContent>
        <Stack spacing={2}>
          <Alert severity="warning">
            Esta contraseña temporal se muestra <strong>una sola vez</strong>. Entréguela por un canal seguro; la persona deberá
            cambiarla en su primer ingreso.
          </Alert>
          <Typography variant="body2" color="text.secondary">
            Correo
          </Typography>
          <Typography sx={{ fontWeight: 600 }}>{cuenta?.correo}</Typography>
          <Typography variant="body2" color="text.secondary">
            Contraseña temporal
          </Typography>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Typography component="code" sx={{ fontFamily: 'monospace', fontSize: 18, letterSpacing: 1 }}>
              {cuenta?.passwordTemporal}
            </Typography>
            <Tooltip title={copiado ? 'Copiada' : 'Copiar'}>
              <IconButton onClick={() => void copiar()} aria-label="Copiar contraseña temporal">
                <ContentCopyIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button
          variant="contained"
          onClick={() => {
            setCopiado(false)
            onCerrar()
          }}
        >
          Ya la guardé
        </Button>
      </DialogActions>
    </Dialog>
  )
}
