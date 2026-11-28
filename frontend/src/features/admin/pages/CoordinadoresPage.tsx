import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { extraerError } from '@/shared/api/errores'
import type { CoordinadorAdminDto, CuentaCreadaDto } from '@/shared/api/types'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { adminApi } from '../api'
import { CrearCoordinadorDialog } from '../components/CrearCoordinadorDialog'
import { CuentaCreadaDialog } from '../components/CuentaCreadaDialog'

type Accion = { tipo: 'estado' | 'password'; coordinador: CoordinadorAdminDto }

/** HU-A03: cuentas de coordinador por universidad. */
export function CoordinadoresPage() {
  const queryClient = useQueryClient()
  const notificar = useNotificar()
  const [universidadId, setUniversidadId] = useState('')
  const [creando, setCreando] = useState(false)
  const [accion, setAccion] = useState<Accion | null>(null)
  const [cuenta, setCuenta] = useState<{ datos: CuentaCreadaDto; titulo: string } | null>(null)

  const universidades = useQuery({ queryKey: ['catalogos', 'universidades'], queryFn: adminApi.universidades })
  const lista = useQuery({
    queryKey: ['admin', 'coordinadores', universidadId],
    queryFn: () => adminApi.coordinadores(universidadId || undefined),
  })
  const refrescar = () => queryClient.invalidateQueries({ queryKey: ['admin', 'coordinadores'] })

  const ejecutar = useMutation({
    mutationFn: async ({ tipo, coordinador }: Accion) => {
      if (tipo === 'estado') {
        await adminApi.cambiarEstadoCoordinador(coordinador.id, !coordinador.activo)
        return null
      }
      return adminApi.restablecerPassword(coordinador.id)
    },
    onSuccess: async (resultado, { tipo, coordinador }) => {
      setAccion(null)
      await refrescar()
      if (resultado) setCuenta({ datos: resultado, titulo: 'Contraseña restablecida' })
      else notificar.exito(`${coordinador.nombreCompleto} ${coordinador.activo ? 'desactivado' : 'reactivado'}.`)
      if (tipo === 'password') notificar.info('La contraseña anterior ya no funciona.')
    },
    onError: (causa) => notificar.error(extraerError(causa).mensaje),
  })

  const columnas: ColumnaTabla<CoordinadorAdminDto>[] = [
    {
      id: 'nombre',
      encabezado: 'Coordinador',
      render: (c) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>{c.nombreCompleto}</Typography>
          <Typography variant="body2" color="text.secondary">{c.correo}</Typography>
        </Box>
      ),
    },
    { id: 'universidad', encabezado: 'Universidad', render: (c) => c.universidad.nombre },
    {
      id: 'estado',
      encabezado: 'Estado',
      render: (c) => <Chip size="small" label={c.activo ? 'Activo' : 'Inactivo'} color={c.activo ? 'success' : 'default'} />,
    },
    {
      id: 'acciones',
      encabezado: 'Acciones',
      alinear: 'right',
      render: (c) => (
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
          <Button size="small" onClick={() => setAccion({ tipo: 'password', coordinador: c })}>
            Restablecer contraseña
          </Button>
          <Button size="small" color={c.activo ? 'error' : 'primary'} onClick={() => setAccion({ tipo: 'estado', coordinador: c })}>
            {c.activo ? 'Desactivar' : 'Reactivar'}
          </Button>
        </Stack>
      ),
    },
  ]

  const objetivo = accion?.coordinador
  return (
    <>
      <EncabezadoPagina
        titulo="Coordinadores"
        subtitulo="Cada universidad necesita un coordinador que apruebe empresas y verifique estudiantes."
        acciones={
          <Button variant="contained" onClick={() => setCreando(true)} disabled={!universidades.data?.length}>
            Nuevo coordinador
          </Button>
        }
      />
      <TextField
        select
        size="small"
        label="Universidad"
        value={universidadId}
        onChange={(e) => setUniversidadId(e.target.value)}
        sx={{ mb: 2, minWidth: 280 }}
        slotProps={{ select: { displayEmpty: true }, inputLabel: { shrink: true } }}
      >
        <MenuItem value="">Todas</MenuItem>
        {(universidades.data ?? []).map((u) => (
          <MenuItem key={u.id} value={u.id}>
            {u.siglas} · {u.nombre}
          </MenuItem>
        ))}
      </TextField>

      <TablaDatos
        etiqueta="Coordinadores"
        columnas={columnas}
        filas={lista.data}
        idFila={(c) => c.id}
        cargando={lista.isPending}
        error={lista.isError ? extraerError(lista.error).mensaje : null}
        onReintentar={() => void lista.refetch()}
        vacio={{ titulo: 'No hay coordinadores', descripcion: 'Cree el primero con "Nuevo coordinador".' }}
      />

      <CrearCoordinadorDialog
        abierto={creando}
        universidades={universidades.data ?? []}
        universidadInicial={universidadId || undefined}
        onCerrar={() => setCreando(false)}
        onCreado={async (datos) => {
          setCreando(false)
          await refrescar()
          setCuenta({ datos, titulo: 'Coordinador creado' })
        }}
      />
      <CuentaCreadaDialog cuenta={cuenta?.datos ?? null} titulo={cuenta?.titulo ?? ''} onCerrar={() => setCuenta(null)} />
      <ConfirmDialog
        abierto={accion !== null}
        titulo={
          accion?.tipo === 'password' ? 'Restablecer contraseña' : objetivo?.activo ? 'Desactivar coordinador' : 'Reactivar coordinador'
        }
        mensaje={
          accion?.tipo === 'password' ? (
            <>Se generará una nueva contraseña temporal para <strong>{objetivo?.nombreCompleto}</strong>; la actual dejará de funcionar.</>
          ) : objetivo?.activo ? (
            <><strong>{objetivo?.nombreCompleto}</strong> no podrá iniciar sesión hasta que lo reactive.</>
          ) : (
            <><strong>{objetivo?.nombreCompleto}</strong> podrá volver a iniciar sesión.</>
          )
        }
        textoConfirmar={accion?.tipo === 'password' ? 'Restablecer' : objetivo?.activo ? 'Desactivar' : 'Reactivar'}
        peligro={accion?.tipo === 'estado' && objetivo?.activo}
        cargando={ejecutar.isPending}
        onConfirmar={() => accion && ejecutar.mutate(accion)}
        onCancelar={() => setAccion(null)}
      />
    </>
  )
}
