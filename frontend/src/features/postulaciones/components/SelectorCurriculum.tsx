import MenuItem from '@mui/material/MenuItem'
import TextField from '@mui/material/TextField'
import type { CurriculumDto, Guid } from '@/shared/api/types'

interface Props {
  lista: CurriculumDto[]
  /** Id efectivo (ver `curriculumEfectivo`). */
  valor: Guid
  onChange: (id: Guid) => void
  error?: string
}

export function SelectorCurriculum({ lista, valor, onChange, error }: Props) {
  return (
    <TextField
      select
      label="Currículum"
      value={valor}
      onChange={(evento) => onChange(evento.target.value)}
      error={Boolean(error)}
      helperText={error ?? 'Es el que verá la empresa.'}
      fullWidth
    >
      {lista.map((cv) => (
        <MenuItem key={cv.id} value={cv.id}>
          {cv.nombreVersion}
          {cv.esPrincipal ? ' (principal)' : ''}
        </MenuItem>
      ))}
    </TextField>
  )
}
