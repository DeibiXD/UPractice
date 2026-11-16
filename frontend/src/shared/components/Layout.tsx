import AppBar from '@mui/material/AppBar'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Container from '@mui/material/Container'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '@/shared/auth/useAuth'

/** Barra superior + contenido. Cada paquete puede agregar aquí su menú de navegación. */
export function Layout() {
  const { usuario, logout } = useAuth()
  const navigate = useNavigate()

  const salir = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="static">
        <Toolbar sx={{ gap: 2 }}>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            UPractice
          </Typography>
          {usuario && (
            <>
              <Typography variant="body2">{usuario.nombreCompleto}</Typography>
              <Chip label={usuario.rol} size="small" color="secondary" />
            </>
          )}
          <Button color="inherit" onClick={salir}>
            Cerrar sesión
          </Button>
        </Toolbar>
      </AppBar>
      <Container sx={{ py: 3 }}>
        <Outlet />
      </Container>
    </Box>
  )
}
