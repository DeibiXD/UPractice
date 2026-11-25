import MenuIcon from '@mui/icons-material/Menu'
import AppBar from '@mui/material/AppBar'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Container from '@mui/material/Container'
import Drawer from '@mui/material/Drawer'
import IconButton from '@mui/material/IconButton'
import List from '@mui/material/List'
import ListItemButton from '@mui/material/ListItemButton'
import ListItemText from '@mui/material/ListItemText'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { useMemo, useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { etiquetaRol } from '@/shared/auth/etiquetasRol'
import { useAuth } from '@/shared/auth/useAuth'
import { menuParaRol, type ContextoLayout, type ItemMenu } from '@/shared/navegacion/menu'

/**
 * Barra superior con el menú del rol (en pantallas pequeñas, un cajón lateral) y el contenido.
 * El menú completo se arma en src/app/menu.ts; aquí solo se filtra por el rol del usuario.
 */
export function Layout({ menu }: { menu: ItemMenu[] }) {
  const { usuario, logout } = useAuth()
  const navigate = useNavigate()
  const [cajonAbierto, setCajonAbierto] = useState(false)

  const menuRol = useMemo(() => (usuario ? menuParaRol(menu, usuario.rol) : []), [menu, usuario])
  const enlaces = [{ etiqueta: 'Inicio', ruta: '/' }, ...menuRol]

  const salir = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const contexto: ContextoLayout = { menuRol }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="static">
        <Toolbar sx={{ gap: 1 }}>
          <IconButton
            color="inherit"
            edge="start"
            aria-label="Abrir menú"
            onClick={() => setCajonAbierto(true)}
            sx={{ display: { md: 'none' } }}
          >
            <MenuIcon />
          </IconButton>

          <Typography
            variant="h6"
            component={Link}
            to="/"
            sx={{ color: 'inherit', textDecoration: 'none', mr: 2 }}
          >
            UPractice
          </Typography>

          <Box
            component="nav"
            aria-label="Principal"
            sx={{ display: { xs: 'none', md: 'flex' }, gap: 0.5, flexGrow: 1 }}
          >
            {enlaces.map((enlace) => (
              <Button
                key={enlace.ruta}
                color="inherit"
                component={NavLink}
                to={enlace.ruta}
                end={enlace.ruta === '/'}
                sx={{ '&.active': { bgcolor: 'rgba(255,255,255,0.18)' } }}
              >
                {enlace.etiqueta}
              </Button>
            ))}
          </Box>
          <Box sx={{ flexGrow: 1, display: { md: 'none' } }} />

          {usuario && (
            <>
              <Typography variant="body2" sx={{ display: { xs: 'none', sm: 'block' } }}>
                {usuario.nombreCompleto}
              </Typography>
              <Chip
                label={etiquetaRol(usuario.rol)}
                size="small"
                color="secondary"
                sx={{ display: { xs: 'none', sm: 'inline-flex' } }}
              />
            </>
          )}
          <Button color="inherit" onClick={salir}>
            Salir
          </Button>
        </Toolbar>
      </AppBar>

      <Drawer open={cajonAbierto} onClose={() => setCajonAbierto(false)}>
        <Box sx={{ width: 260 }} role="presentation" onClick={() => setCajonAbierto(false)}>
          <List component="nav" aria-label="Principal">
            {enlaces.map((enlace) => (
              <ListItemButton key={enlace.ruta} component={NavLink} to={enlace.ruta} end={enlace.ruta === '/'}>
                <ListItemText primary={enlace.etiqueta} />
              </ListItemButton>
            ))}
          </List>
        </Box>
      </Drawer>

      <Container sx={{ py: 3 }}>
        <Outlet context={contexto} />
      </Container>
    </Box>
  )
}
