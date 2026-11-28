import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { NotificacionesProvider } from '@/shared/notificaciones/NotificacionesProvider'

interface Opciones {
  /** Patrón de ruta del elemento (p. ej. "/estudiante/postulaciones/:id"). */
  patron?: string
  /** URL inicial (p. ej. "/estudiante/postulaciones/p1"). */
  ubicacion?: string
  /** Otras rutas, para comprobar a dónde navega la pantalla: se pintan como texto. */
  destinos?: { path: string; texto: string }[]
}

/** Renderiza con React Query, notificaciones y router, igual que en la app (sin red: la API se simula en cada prueba). */
export function renderEnRuta(elemento: ReactElement, { patron = '/', ubicacion = '/', destinos = [] }: Opciones = {}) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={cliente}>
      <NotificacionesProvider>
        <MemoryRouter initialEntries={[ubicacion]}>
          <Routes>
            <Route path={patron} element={elemento} />
            {destinos.map((destino) => (
              <Route key={destino.path} path={destino.path} element={<div>{destino.texto}</div>} />
            ))}
          </Routes>
        </MemoryRouter>
      </NotificacionesProvider>
    </QueryClientProvider>,
  )
}
