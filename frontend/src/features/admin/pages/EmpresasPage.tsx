import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import Tab from '@mui/material/Tab'
import TablePagination from '@mui/material/TablePagination'
import Tabs from '@mui/material/Tabs'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { extraerError } from '@/shared/api/errores'
import type { EmpresaAdminDto } from '@/shared/api/types'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { formatearFecha } from '@/shared/lib/formatos'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { adminApi } from '../api'
import { SuspenderEmpresaDialog } from '../components/SuspenderEmpresaDialog'

type Filtro = 'todas' | 'activas' | 'suspendidas'

/** HU-A04: el SuperAdmin supervisa las empresas y suspende las que incumplen. No ve sus vacantes ni postulaciones (D-14). */
export function EmpresasPage() {
  const queryClient = useQueryClient()
  const notificar = useNotificar()
  const [filtro, setFiltro] = useState<Filtro>('todas')
  const [texto, setTexto] = useState('')
  const [busqueda, setBusqueda] = useState('')
  const [pagina, setPagina] = useState(0)
  const [tamanoPagina, setTamanoPagina] = useState(10)
  const [porSuspender, setPorSuspender] = useState<EmpresaAdminDto | null>(null)
  const [porReactivar, setPorReactivar] = useState<EmpresaAdminDto | null>(null)

  // La búsqueda se aplica 400 ms después de dejar de escribir.
  useEffect(() => {
    const t = setTimeout(() => {
      setBusqueda(texto.trim())
      setPagina(0)
    }, 400)
    return () => clearTimeout(t)
  }, [texto])

  const parametros = {
    busqueda: busqueda || undefined,
    activa: filtro === 'todas' ? undefined : filtro === 'activas',
    pagina: pagina + 1,
    tamanoPagina,
  }
  const lista = useQuery({
    queryKey: ['admin', 'empresas', parametros],
    queryFn: () => adminApi.empresas(parametros),
    placeholderData: keepPreviousData,
  })
  const refrescar = () => queryClient.invalidateQueries({ queryKey: ['admin', 'empresas'] })

  const reactivar = useMutation({
    mutationFn: (e: EmpresaAdminDto) => adminApi.cambiarEstadoEmpresa(e.id, { activa: true }),
    onSuccess: async (_, e) => {
      setPorReactivar(null)
      await refrescar()
      notificar.exito(`${e.nombreComercial} reactivada.`)
    },
    onError: (causa) => notificar.error(extraerError(causa).mensaje),
  })

  const columnas: ColumnaTabla<EmpresaAdminDto>[] = [
    {
      id: 'empresa',
      encabezado: 'Empresa',
      render: (e) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>{e.nombreComercial}</Typography>
          <Typography variant="body2" color="text.secondary">{e.razonSocial}</Typography>
        </Box>
      ),
    },
    { id: 'rtn', encabezado: 'RTN', render: (e) => e.rtn },
    { id: 'sector', encabezado: 'Sector', render: (e) => e.sector },
    { id: 'registro', encabezado: 'Registro', render: (e) => formatearFecha(e.creadoEn) },
    {
      id: 'habilitaciones',
      encabezado: 'Habilitaciones',
      render: (e) =>
        e.habilitaciones.length === 0 ? (
          <Typography variant="body2" color="text.secondary">Ninguna</Typography>
        ) : (
          <Stack direction="row" spacing={0.5} sx={{ flexWrap: 'wrap', rowGap: 0.5 }}>
            {e.habilitaciones.map((h) => (
              <Stack key={h.universidad} direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                <Typography variant="caption">{h.universidad}</Typography>
                <ChipEstado estado={h.estado} />
              </Stack>
            ))}
          </Stack>
        ),
    },
    {
      id: 'estado',
      encabezado: 'Estado',
      render: (e) =>
        e.activa ? (
          <Chip size="small" color="success" label="Activa" />
        ) : (
          <Box>
            <Chip size="small" color="error" label="Suspendida" />
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', maxWidth: 220 }}>
              {e.motivoSuspension}
            </Typography>
          </Box>
        ),
    },
    {
      id: 'acciones',
      encabezado: 'Acciones',
      alinear: 'right',
      render: (e) =>
        e.activa ? (
          <Button size="small" color="error" onClick={() => setPorSuspender(e)}>
            Suspender
          </Button>
        ) : (
          <Button size="small" onClick={() => setPorReactivar(e)}>
            Reactivar
          </Button>
        ),
    },
  ]

  return (
    <>
      <EncabezadoPagina
        titulo="Empresas"
        subtitulo="Supervise las empresas registradas y suspenda las que incumplan las condiciones de uso."
      />
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2, alignItems: { sm: 'center' } }}>
        <TextField
          size="small"
          label="Buscar por nombre, razón social o RTN"
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          sx={{ minWidth: 320 }}
          slotProps={{ htmlInput: { maxLength: 100 } }}
        />
        <Tabs
          value={filtro}
          onChange={(_, v: Filtro) => {
            setFiltro(v)
            setPagina(0)
          }}
        >
          <Tab value="todas" label="Todas" />
          <Tab value="activas" label="Activas" />
          <Tab value="suspendidas" label="Suspendidas" />
        </Tabs>
      </Stack>

      <TablaDatos
        etiqueta="Empresas"
        columnas={columnas}
        filas={lista.data?.items}
        idFila={(e) => e.id}
        cargando={lista.isPending}
        error={lista.isError ? extraerError(lista.error).mensaje : null}
        onReintentar={() => void lista.refetch()}
        vacio={{ titulo: 'No hay empresas que coincidan' }}
      />
      {lista.data && lista.data.totalRegistros > 0 && (
        <TablePagination
          component="div"
          count={lista.data.totalRegistros}
          page={pagina}
          rowsPerPage={tamanoPagina}
          rowsPerPageOptions={[10, 25, 50]}
          onPageChange={(_, p) => setPagina(p)}
          onRowsPerPageChange={(e) => {
            setTamanoPagina(Number(e.target.value))
            setPagina(0)
          }}
          labelRowsPerPage="Por página"
          labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
        />
      )}

      <SuspenderEmpresaDialog
        empresa={porSuspender}
        onCerrar={() => setPorSuspender(null)}
        onSuspendida={async () => {
          const nombre = porSuspender?.nombreComercial
          setPorSuspender(null)
          await refrescar()
          notificar.info(`${nombre} suspendida.`)
        }}
      />
      <ConfirmDialog
        abierto={porReactivar !== null}
        titulo="Reactivar empresa"
        mensaje={
          <>
            <strong>{porReactivar?.nombreComercial}</strong> volverá a la normalidad: sus usuarios podrán iniciar sesión y sus
            vacantes se verán de nuevo.
          </>
        }
        textoConfirmar="Reactivar"
        cargando={reactivar.isPending}
        onConfirmar={() => porReactivar && reactivar.mutate(porReactivar)}
        onCancelar={() => setPorReactivar(null)}
      />
    </>
  )
}
