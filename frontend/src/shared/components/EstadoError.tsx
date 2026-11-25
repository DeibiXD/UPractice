import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'

interface Props {
  mensaje: string
  onReintentar?: () => void
}

/** Para cuando falla la carga de una pantalla o de una lista. */
export function EstadoError({ mensaje, onReintentar }: Props) {
  return (
    <Alert
      severity="error"
      action={
        onReintentar && (
          <Button color="inherit" size="small" onClick={onReintentar}>
            Reintentar
          </Button>
        )
      }
    >
      {mensaje}
    </Alert>
  )
}
