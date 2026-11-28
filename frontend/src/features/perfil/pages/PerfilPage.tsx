import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Autocomplete from '@mui/material/Autocomplete'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import FormControlLabel from '@mui/material/FormControlLabel'
import MenuItem from '@mui/material/MenuItem'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Switch from '@mui/material/Switch'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { aplicarErrores, extraerError } from '@/shared/api/errores'
import type { HabilidadDto, PerfilEstudianteDto } from '@/shared/api/types'
import { Cargando } from '@/shared/components/Cargando'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { EstadoError } from '@/shared/components/EstadoError'
import { formatearFechaHora } from '@/shared/lib/formatos'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { perfilApi } from '../api'
import { esquemaPerfilAcademico, type PerfilAcademicoFormulario } from '../schemas/perfil'

const CLAVE_PERFIL = ['perfil'] as const

/** HU-02: datos académicos (para verificación), habilidades y privacidad del índice. */
export function PerfilPage() {
  const perfil = useQuery({ queryKey: CLAVE_PERFIL, queryFn: perfilApi.obtener })

  if (perfil.isPending) return <Cargando />
  if (perfil.isError)
    return <EstadoError mensaje={extraerError(perfil.error).mensaje} onReintentar={() => void perfil.refetch()} />

  const datos = perfil.data
  return (
    <>
      <EncabezadoPagina titulo="Mi perfil" subtitulo={`${datos.nombreCompleto} · ${datos.universidad.nombre}`} />
      <Stack spacing={3}>
        <EstadoVerificacionAviso perfil={datos} />
        <DatosAcademicos perfil={datos} />
        <Habilidades key={datos.habilidades.map((h) => h.id).join(',')} perfil={datos} />
        <Privacidad perfil={datos} />
      </Stack>
    </>
  )
}

function EstadoVerificacionAviso({ perfil }: { perfil: PerfilEstudianteDto }) {
  if (perfil.numeroCuenta === null)
    return <Alert severity="info">Complete sus datos académicos para que la coordinación de su universidad los verifique.</Alert>

  return (
    <Alert
      severity={perfil.estadoVerificacion === 'Verificado' ? 'success' : perfil.estadoVerificacion === 'Rechazado' ? 'error' : 'warning'}
      icon={false}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: perfil.observacionVerificacion ? 1 : 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 600 }}>Estado de verificación:</Typography>
        <ChipEstado estado={perfil.estadoVerificacion} />
        {perfil.fechaVerificacion && (
          <Typography variant="body2" color="text.secondary">{formatearFechaHora(perfil.fechaVerificacion)}</Typography>
        )}
      </Stack>
      {perfil.estadoVerificacion === 'Pendiente' && 'La coordinación revisará sus datos. Mientras tanto puede corregirlos.'}
      {perfil.estadoVerificacion === 'Verificado' && 'Sus datos académicos están verificados y ya no se pueden cambiar.'}
      {perfil.estadoVerificacion === 'Rechazado' && (
        <>
          <strong>Observación de la coordinación:</strong> {perfil.observacionVerificacion}
          <br />
          Corrija sus datos y guárdelos para volver a solicitar la verificación.
        </>
      )}
    </Alert>
  )
}

function DatosAcademicos({ perfil }: { perfil: PerfilEstudianteDto }) {
  const queryClient = useQueryClient()
  const notificar = useNotificar()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const bloqueado = perfil.estadoVerificacion === 'Verificado'

  const carreras = useQuery({
    queryKey: ['catalogos', 'carreras', perfil.universidad.id],
    queryFn: () => perfilApi.carreras(perfil.universidad.id),
  })

  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isValid, isSubmitting, isDirty },
  } = useForm<PerfilAcademicoFormulario>({
    resolver: zodResolver(esquemaPerfilAcademico),
    mode: 'onChange',
    defaultValues: {
      numeroCuenta: perfil.numeroCuenta ?? '',
      carreraId: perfil.carrera?.id ?? '',
      indiceAcademico: perfil.indiceAcademico ?? Number.NaN,
    },
  })

  // Al recibir el perfil guardado, el formulario vuelve a quedar "limpio" con esos valores.
  useEffect(() => {
    reset({
      numeroCuenta: perfil.numeroCuenta ?? '',
      carreraId: perfil.carrera?.id ?? '',
      indiceAcademico: perfil.indiceAcademico ?? Number.NaN,
    })
  }, [perfil, reset])

  const alEnviar = handleSubmit(async (datos) => {
    setErrorGeneral(null)
    try {
      const actualizado = await perfilApi.actualizarAcademico(datos)
      queryClient.setQueryData(CLAVE_PERFIL, actualizado)
      notificar.exito('Datos académicos guardados. Quedan pendientes de verificación.')
    } catch (causa) {
      const error = extraerError(causa)
      aplicarErrores(error, setError)
      if (Object.keys(error.campos).length === 0) setErrorGeneral(error.mensaje)
    }
  })

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" component="h2" gutterBottom>
        Datos académicos
      </Typography>
      <Stack component="form" spacing={2.5} onSubmit={alEnviar} noValidate sx={{ maxWidth: 520 }}>
        {errorGeneral && <Alert severity="error">{errorGeneral}</Alert>}
        <TextField
          label="Número de cuenta"
          inputMode="numeric"
          disabled={bloqueado}
          {...register('numeroCuenta')}
          error={Boolean(errors.numeroCuenta)}
          helperText={errors.numeroCuenta?.message}
        />
        <Controller
          name="carreraId"
          control={control}
          render={({ field }) => (
            <TextField
              select
              label="Carrera"
              disabled={bloqueado || carreras.isPending}
              {...field}
              error={Boolean(errors.carreraId) || carreras.isError}
              helperText={
                errors.carreraId?.message ?? (carreras.isError ? 'No se pudo cargar el catálogo de carreras.' : undefined)
              }
            >
              {(carreras.data ?? []).map((c) => (
                <MenuItem key={c.id} value={c.id}>
                  {c.nombre} · {c.facultad}
                </MenuItem>
              ))}
              {/* Si la carrera guardada ya no está activa, se sigue mostrando. */}
              {perfil.carrera && !carreras.data?.some((c) => c.id === perfil.carrera?.id) && (
                <MenuItem value={perfil.carrera.id}>{perfil.carrera.nombre}</MenuItem>
              )}
            </TextField>
          )}
        />
        <TextField
          label="Índice académico"
          type="number"
          slotProps={{ htmlInput: { step: '0.01', min: 0, max: 100 } }}
          disabled={bloqueado}
          {...register('indiceAcademico', { valueAsNumber: true })}
          error={Boolean(errors.indiceAcademico)}
          helperText={errors.indiceAcademico?.message}
        />
        {!bloqueado && (
          <Box>
            <Button type="submit" variant="contained" disabled={!isValid || !isDirty || isSubmitting}>
              {isSubmitting ? 'Guardando…' : perfil.numeroCuenta === null ? 'Enviar a verificación' : 'Guardar cambios'}
            </Button>
          </Box>
        )}
      </Stack>
    </Paper>
  )
}

function Habilidades({ perfil }: { perfil: PerfilEstudianteDto }) {
  const queryClient = useQueryClient()
  const notificar = useNotificar()
  // El padre le da una key con las habilidades guardadas: al guardar, el componente se reinicia con ellas.
  const [seleccion, setSeleccion] = useState<HabilidadDto[]>(perfil.habilidades)

  const catalogo = useQuery({ queryKey: ['catalogos', 'habilidades'], queryFn: perfilApi.habilidades })
  const guardar = useMutation({
    mutationFn: () => perfilApi.actualizarHabilidades({ habilidadesIds: seleccion.map((h) => h.id) }),
    onSuccess: (actualizado) => {
      queryClient.setQueryData(CLAVE_PERFIL, actualizado)
      notificar.exito('Habilidades guardadas.')
    },
    onError: (causa) => notificar.error(extraerError(causa).mensaje),
  })

  const sinCambios =
    seleccion.length === perfil.habilidades.length && seleccion.every((h) => perfil.habilidades.some((p) => p.id === h.id))

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" component="h2" gutterBottom>
        Habilidades
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Se usan para calcular si cumple los requisitos de cada vacante. Puede cambiarlas aunque su perfil esté verificado.
      </Typography>
      <Stack spacing={2} sx={{ maxWidth: 520 }}>
        <Autocomplete
          multiple
          options={catalogo.data ?? []}
          loading={catalogo.isPending}
          value={seleccion}
          onChange={(_, valor) => setSeleccion(valor)}
          getOptionLabel={(h) => h.nombre}
          isOptionEqualToValue={(a, b) => a.id === b.id}
          renderInput={(params) => <TextField {...params} label="Habilidades" placeholder="Buscar…" />}
        />
        <Box>
          <Button variant="contained" onClick={() => guardar.mutate()} disabled={sinCambios || guardar.isPending}>
            {guardar.isPending ? 'Guardando…' : 'Guardar habilidades'}
          </Button>
        </Box>
      </Stack>
    </Paper>
  )
}

function Privacidad({ perfil }: { perfil: PerfilEstudianteDto }) {
  const queryClient = useQueryClient()
  const notificar = useNotificar()
  const sinPerfil = perfil.numeroCuenta === null

  const cambiar = useMutation({
    mutationFn: (mostrarIndice: boolean) => perfilApi.actualizarPrivacidad({ mostrarIndice }),
    onSuccess: (_, mostrarIndice) => {
      queryClient.setQueryData<PerfilEstudianteDto>(CLAVE_PERFIL, (actual) =>
        actual ? { ...actual, privacidad: { mostrarIndice } } : actual,
      )
      notificar.exito(mostrarIndice ? 'Las empresas verán su índice.' : 'Las empresas ya no verán su índice.')
    },
    onError: (causa) => notificar.error(extraerError(causa).mensaje),
  })

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" component="h2" gutterBottom>
        Privacidad
      </Typography>
      <FormControlLabel
        control={
          <Switch
            checked={perfil.privacidad.mostrarIndice}
            disabled={sinPerfil || cambiar.isPending}
            onChange={(e) => cambiar.mutate(e.target.checked)}
          />
        }
        label="Mostrar mi índice académico a las empresas donde me postulo"
      />
      {sinPerfil && (
        <Typography variant="body2" color="text.secondary">
          Disponible cuando complete sus datos académicos.
        </Typography>
      )}
    </Paper>
  )
}
