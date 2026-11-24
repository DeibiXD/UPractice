import CssBaseline from '@mui/material/CssBaseline'
import { ThemeProvider } from '@mui/material/styles'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { tema } from '@/app/tema'
import { AuthProvider } from '@/shared/auth/AuthContext'
import { NotificacionesProvider } from '@/shared/notificaciones/NotificacionesProvider'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } },
})

export function Providers({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={tema}>
        <CssBaseline />
        <NotificacionesProvider>
          <AuthProvider>{children}</AuthProvider>
        </NotificacionesProvider>
      </ThemeProvider>
    </QueryClientProvider>
  )
}
