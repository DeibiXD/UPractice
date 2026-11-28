import Pagination from '@mui/material/Pagination'
import Stack from '@mui/material/Stack'

interface Props {
  pagina: number
  totalPaginas: number
  onCambiar: (pagina: number) => void
}

/** Paginador para las respuestas PaginaResultado. No se muestra si hay una sola página. */
export function Paginacion({ pagina, totalPaginas, onCambiar }: Props) {
  if (totalPaginas <= 1) return null
  return (
    <Stack sx={{ alignItems: 'center', mt: 2 }}>
      <Pagination
        count={totalPaginas}
        page={pagina}
        color="primary"
        onChange={(_evento, nueva) => onCambiar(nueva)}
      />
    </Stack>
  )
}
