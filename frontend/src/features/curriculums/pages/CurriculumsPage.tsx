import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { extraerError } from '@/shared/api/errores'
import type { CurriculumDto } from '@/shared/api/types'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { TablaDatos, type ColumnaTabla } from '@/shared/components/TablaDatos'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { curriculumsApi } from '../api'
import { SubirCurriculumDialog } from '../components/SubirCurriculumDialog'
import { MAXIMO_ACTIVOS } from '../schemas/curriculum'

const CLAVE = ['curriculums'] as const

/** HU-03: versiones del CV. La principal se preselecciona al postular. */
export function CurriculumsPage() {
  const queryClient = useQueryClient()
  const notificar = useNotificar()
  const [subiendo, setSubiendo] = useState(false)
  const [porEliminar, setPorEliminar] = useState<CurriculumDto | null>(null)

  const lista = useQuery({ queryKey: CLAVE, queryFn: curriculumsApi.listar })
  const refrescar = () => queryClient.invalidateQueries({ queryKey: CLAVE })
  const alFallar = (causa: unknown) => notificar.error(extraerError(causa).mensaje)

  const principal = useMutation({
    mutationFn: curriculumsApi.marcarPrincipal,
    onSuccess: async () => {
      await refrescar()
      notificar.exito('Versión principal actualizada.')
    },
    onError: alFallar,
  })
  const eliminar = useMutation({
    mutationFn: curriculumsApi.eliminar,
    onSuccess: async () => {
      setPorEliminar(null)
      await refrescar()
      notificar.exito('Versión eliminada.')
    },
    onError: alFallar,
  })
  const descargar = useMutation({
    // La URL firmada vence en 5 minutos: se pide en el momento. La pestaña se abre en el mismo clic (si se abriera
    // después de esperar la respuesta, el navegador la bloquearía como ventana emergente) y luego se le asigna la URL.
    mutationFn: async ({ id }: { id: string; ventana: Window | null }) => curriculumsApi.urlDescarga(id),
    onSuccess: (url, { ventana }) => {
      if (ventana) ventana.location.href = url.url
      else window.location.assign(url.url)
    },
    onError: (causa, { ventana }) => {
      ventana?.close()
      alFallar(causa)
    },
  })
  const abrirDescarga = (id: string) => {
    const ventana = window.open('', '_blank')
    if (ventana) ventana.opener = null
    descargar.mutate({ id, ventana })
  }

  const total = lista.data?.length ?? 0
  const lleno = total >= MAXIMO_ACTIVOS

  const columnas: ColumnaTabla<CurriculumDto>[] = [
    {
      id: 'nombre',
      encabezado: 'Versión',
      render: (c) => (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <span>{c.nombreVersion}</span>
          {c.esPrincipal && <Chip size="small" color="primary" label="Principal" />}
        </Stack>
      ),
    },
    {
      id: 'acciones',
      encabezado: 'Acciones',
      alinear: 'right',
      render: (c) => (
        <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
          <Button size="small" onClick={() => abrirDescarga(c.id)} disabled={descargar.isPending}>
            Descargar
          </Button>
          {!c.esPrincipal && (
            <Button size="small" onClick={() => principal.mutate(c.id)} disabled={principal.isPending}>
              Marcar principal
            </Button>
          )}
          <Button size="small" color="error" onClick={() => setPorEliminar(c)}>
            Eliminar
          </Button>
        </Stack>
      ),
    },
  ]

  return (
    <>
      <EncabezadoPagina
        titulo="Mis currículums"
        subtitulo={`Puede tener hasta ${MAXIMO_ACTIVOS} versiones activas (${total}/${MAXIMO_ACTIVOS}).`}
        acciones={
          <Button variant="contained" onClick={() => setSubiendo(true)} disabled={lleno || lista.isPending}>
            Subir versión
          </Button>
        }
      />
      {lleno && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Alcanzó el máximo de versiones. Elimine una para subir otra.
        </Typography>
      )}

      <TablaDatos
        etiqueta="Versiones del CV"
        columnas={columnas}
        filas={lista.data}
        idFila={(c) => c.id}
        cargando={lista.isPending}
        error={lista.isError ? extraerError(lista.error).mensaje : null}
        onReintentar={() => void lista.refetch()}
        vacio={{
          titulo: 'Aún no ha subido su CV',
          descripcion: 'Suba al menos una versión en PDF para poder postularse a vacantes.',
          accion: (
            <Button variant="contained" onClick={() => setSubiendo(true)}>
              Subir versión
            </Button>
          ),
        }}
      />

      <SubirCurriculumDialog
        abierto={subiendo}
        onCerrar={() => setSubiendo(false)}
        onSubido={async (c) => {
          setSubiendo(false)
          await refrescar()
          notificar.exito(c.esPrincipal ? `"${c.nombreVersion}" quedó como su CV principal.` : `"${c.nombreVersion}" subido.`)
        }}
      />

      <ConfirmDialog
        abierto={porEliminar !== null}
        titulo="Eliminar versión"
        mensaje={
          <>
            ¿Eliminar <strong>{porEliminar?.nombreVersion}</strong>? Dejará de aparecer en su lista, pero las empresas a las que
            ya se postuló con esta versión la conservan.
          </>
        }
        textoConfirmar="Eliminar"
        peligro
        cargando={eliminar.isPending}
        onConfirmar={() => porEliminar && eliminar.mutate(porEliminar.id)}
        onCancelar={() => setPorEliminar(null)}
      />
    </>
  )
}
