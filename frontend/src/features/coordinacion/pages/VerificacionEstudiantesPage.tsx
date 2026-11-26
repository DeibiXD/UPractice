import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import Tab from '@mui/material/Tab'
import TablePagination from '@mui/material/TablePagination'
import Tabs from '@mui/material/Tabs'
import Typography from '@mui/material/Typography'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { extraerError } from '@/shared/api/errores'
import type { EstadoVerificacion, EstudianteVerificacionDto } from '@/shared/api/types'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { verificacionApi } from '../api'
import { RechazarVerificacionDialog } from '../components/RechazarVerificacionDialog'

type Filtro = EstadoVerificacion | 'Todos'
const FILTROS: { valor: Filtro; etiqueta: string }[] = [
  { valor: 'Pendiente', etiqueta: 'Pendientes' },
  { valor: 'Verificado', etiqueta: 'Verificados' },
  { valor: 'Rechazado', etiqueta: 'Rechazados' },
  { valor: 'Todos', etiqueta: 'Todos' },
]

/** HU-C02: la coordinación verifica o rechaza los datos académicos de los estudiantes de su universidad. */
export function VerificacionEstudiantesPage() {
  const queryClient = useQueryClient()
  const notificar = useNotificar()
  const [filtro, setFiltro] = useState<Filtro>('Pendiente')
  const [pagina, setPagina] = useState(0) // TablePagination empieza en 0; la API en 1
  const [tamanoPagina, setTamanoPagina] = useState(10)
  const [porVerificar, setPorVerificar] = useState<EstudianteVerificacionDto | null>(null)
  const [porRechazar, setPorRechazar] = useState<EstudianteVerificacionDto | null>(null)

  const parametros = {
    estadoVerificacion: filtro === 'Todos' ? undefined : filtro,
    pagina: pagina + 1,
    tamanoPagina,
  }
  const lista = useQuery({
    queryKey: ['coordinacion', 'estudiantes', parametros],
    queryFn: () => verificacionApi.listar(parametros),
    placeholderData: keepPreviousData,
  })
  const refrescar = () => queryClient.invalidateQueries({ queryKey: ['coordinacion', 'estudiantes'] })

  const verificar = useMutation({
    mutationFn: (e: EstudianteVerificacionDto) => verificacionApi.resolver(e.usuarioId, { aprobar: true }),
    onSuccess: async (_, e) => {
      setPorVerificar(null)
      await refrescar()
      notificar.exito(`Perfil de ${e.nombreCompleto} verificado.`)
    },
    onError: (causa) => notificar.error(extraerError(causa).mensaje),
  })

  const columnas: ColumnaTabla<EstudianteVerificacionDto>[] = [
    {
      id: 'estudiante',
      encabezado: 'Estudiante',
      render: (e) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>{e.nombreCompleto}</Typography>
          <Typography variant="body2" color="text.secondary">{e.correo}</Typography>
        </Box>
      ),
    },
    { id: 'cuenta', encabezado: 'Número de cuenta', render: (e) => e.numeroCuenta },
    { id: 'carrera', encabezado: 'Carrera', render: (e) => e.carrera },
    { id: 'indice', encabezado: 'Índice', alinear: 'right', render: (e) => e.indiceAcademico.toFixed(2) },
    { id: 'estado', encabezado: 'Estado', render: (e) => <ChipEstado estado={e.estadoVerificacion} /> },
    {
      id: 'acciones',
      encabezado: 'Acciones',
      alinear: 'right',
      render: (e) =>
        e.estadoVerificacion === 'Pendiente' ? (
          <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
            <Button size="small" variant="contained" onClick={() => setPorVerificar(e)}>
              Verificar
            </Button>
            <Button size="small" color="error" onClick={() => setPorRechazar(e)}>
              Rechazar
            </Button>
          </Stack>
        ) : null,
    },
  ]

  return (
    <>
      <EncabezadoPagina
        titulo="Verificación de estudiantes"
        subtitulo="Revise que el número de cuenta, la carrera y el índice coincidan con los registros de su universidad."
      />
      <Tabs
        value={filtro}
        onChange={(_, valor: Filtro) => {
          setFiltro(valor)
          setPagina(0)
        }}
        sx={{ mb: 2 }}
      >
        {FILTROS.map((f) => (
          <Tab key={f.valor} value={f.valor} label={f.etiqueta} />
        ))}
      </Tabs>

      <TablaDatos
        etiqueta="Estudiantes por verificar"
        columnas={columnas}
        filas={lista.data?.items}
        idFila={(e) => e.usuarioId}
        cargando={lista.isPending}
        error={lista.isError ? extraerError(lista.error).mensaje : null}
        onReintentar={() => void lista.refetch()}
        vacio={{
          titulo: filtro === 'Pendiente' ? 'No hay perfiles pendientes' : 'No hay perfiles en este estado',
          descripcion: filtro === 'Pendiente' ? 'Cuando un estudiante envíe sus datos académicos aparecerá aquí.' : undefined,
        }}
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

      <ConfirmDialog
        abierto={porVerificar !== null}
        titulo="Verificar perfil"
        mensaje={
          <>
            ¿Confirma que los datos de <strong>{porVerificar?.nombreCompleto}</strong> (cuenta {porVerificar?.numeroCuenta},{' '}
            {porVerificar?.carrera}, índice {porVerificar?.indiceAcademico.toFixed(2)}) son correctos? Después el estudiante
            ya no podrá cambiarlos.
          </>
        }
        textoConfirmar="Verificar"
        cargando={verificar.isPending}
        onConfirmar={() => porVerificar && verificar.mutate(porVerificar)}
        onCancelar={() => setPorVerificar(null)}
      />
      <RechazarVerificacionDialog
        estudiante={porRechazar}
        onCerrar={() => setPorRechazar(null)}
        onRechazado={async () => {
          const nombre = porRechazar?.nombreCompleto
          setPorRechazar(null)
          await refrescar()
          notificar.info(`Perfil de ${nombre} rechazado con observación.`)
        }}
      />
    </>
  )
}
