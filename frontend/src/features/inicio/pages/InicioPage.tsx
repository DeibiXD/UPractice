import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import Typography from '@mui/material/Typography'
import { Link, useOutletContext } from 'react-router-dom'
import { etiquetaRol } from '@/shared/auth/etiquetasRol'
import { useAuth } from '@/shared/auth/useAuth'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { EstadoVacio } from '@/shared/components/EstadoVacio'
import type { ContextoLayout } from '@/shared/navegacion/menu'

/**
 * Aterrizaje de todos los roles: saludo y una tarjeta por cada sección del menú de su rol.
 * Crece sola: cuando una feature agrega su entrada al menú (src/app/menu.ts), aparece aquí.
 */
export function InicioPage() {
  const { usuario } = useAuth()
  const accesos = useOutletContext<ContextoLayout | undefined>()?.menuRol ?? []

  if (!usuario) return null

  const organizacion = usuario.empresa?.nombre ?? usuario.universidad?.nombre
  const subtitulo = organizacion ? `${etiquetaRol(usuario.rol)} · ${organizacion}` : etiquetaRol(usuario.rol)

  return (
    <>
      <EncabezadoPagina titulo={`Hola, ${usuario.nombreCompleto}`} subtitulo={subtitulo} />

      {accesos.length === 0 ? (
        <EstadoVacio
          titulo="Aún no hay secciones disponibles"
          descripcion="Las secciones de su rol aparecerán aquí a medida que se habiliten."
        />
      ) : (
        <Box
          sx={{
            display: 'grid',
            gap: 2,
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' },
          }}
        >
          {accesos.map((acceso) => (
            <Paper key={acceso.ruta} variant="outlined" sx={{ p: 2.5, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              <Typography variant="h6" component="h2">
                {acceso.etiqueta}
              </Typography>
              {acceso.descripcion && <Typography color="text.secondary">{acceso.descripcion}</Typography>}
              <Box sx={{ mt: 'auto' }}>
                <Button component={Link} to={acceso.ruta} variant="contained" size="small">
                  Abrir
                </Button>
              </Box>
            </Paper>
          ))}
        </Box>
      )}
    </>
  )
}
