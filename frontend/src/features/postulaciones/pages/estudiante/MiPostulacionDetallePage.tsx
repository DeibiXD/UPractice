import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import Typography from '@mui/material/Typography'
import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { extraerError } from '@/shared/api/errores'
import type { AccionPostulacion } from '@/shared/api/types'
import { ChipEstado } from '@/shared/components/ChipEstado'
import { Cargando } from '@/shared/components/Cargando'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { EncabezadoPagina } from '@/shared/components/EncabezadoPagina'
import { EstadoError } from '@/shared/components/EstadoError'
import { formatearFechaHora } from '@/shared/lib/formatos'
import { useNotificar } from '@/shared/notificaciones/useNotificar'
import { HistorialLista } from '../../components/HistorialLista'
import { SeleccionarCurriculumDialog } from '../../components/SeleccionarCurriculumDialog'
import { useMiPostulacion, useReactivar, useResponderOferta, useRetirar } from '../../hooks'
import { ETIQUETA_ACCION } from '../../lib/transiciones'

function Dato({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <Box>
      <Typography variant="overline" color="text.secondary">
        {etiqueta}
      </Typography>
      <Typography component="div">{children}</Typography>
    </Box>
  )
}

/** HU-06: detalle de una postulación con su historial y las acciones que el backend permite en su estado. */
export function MiPostulacionDetallePage() {
  const { id = '' } = useParams()
  const consulta = useMiPostulacion(id)
  const retirar = useRetirar(id)
  const reactivar = useReactivar(id)
  const responderOferta = useResponderOferta(id)
  const notificar = useNotificar()
  const [dialogo, setDialogo] = useState<AccionPostulacion | null>(null)

  if (consulta.isPending) return <Cargando />
  if (consulta.isError) {
    return <EstadoError mensaje={extraerError(consulta.error).mensaje} onReintentar={() => void consulta.refetch()} />
  }

  const postulacion = consulta.data
  const titulo = postulacion.vacante.titulo

  const ejecutar = async (accion: () => Promise<unknown>, exito: string) => {
    try {
      await accion()
      notificar.exito(exito)
    } catch (causa) {
      notificar.error(extraerError(causa).mensaje)
    } finally {
      setDialogo(null)
    }
  }

  return (
    <>
      <Button component={Link} to="/estudiante/postulaciones" size="small" sx={{ mb: 1 }}>
        ← Mis postulaciones
      </Button>

      <EncabezadoPagina
        titulo={titulo}
        subtitulo={postulacion.empresa.nombre}
        acciones={postulacion.acciones.map((accion) => (
          <Button
            key={accion}
            variant={accion === 'AceptarOferta' || accion === 'Reactivar' ? 'contained' : 'outlined'}
            color={accion === 'Retirar' ? 'error' : accion === 'AceptarOferta' ? 'success' : 'primary'}
            onClick={() => setDialogo(accion)}
          >
            {ETIQUETA_ACCION[accion]}
          </Button>
        ))}
      />

      <Paper variant="outlined" sx={{ p: 3, mb: 3 }}>
        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(4, 1fr)' } }}>
          <Dato etiqueta="Estado">
            <ChipEstado estado={postulacion.estado} />
          </Dato>
          <Dato etiqueta="Postulación">{formatearFechaHora(postulacion.fechaPostulacion)}</Dato>
          <Dato etiqueta="Último cambio">{formatearFechaHora(postulacion.fechaUltimoCambio)}</Dato>
          <Dato etiqueta="Currículum enviado">{postulacion.curriculum.nombreVersion}</Dato>
        </Box>
      </Paper>

      <Paper variant="outlined" sx={{ p: 3 }}>
        <Typography variant="h6" component="h2" sx={{ mb: 2 }}>
          Historial
        </Typography>
        <HistorialLista items={postulacion.historial} />
      </Paper>

      <ConfirmDialog
        abierto={dialogo === 'Retirar'}
        titulo="Retirar postulación"
        mensaje={`¿Retirar su postulación a «${titulo}»? Podrá reactivarla después mientras la vacante siga disponible.`}
        textoConfirmar="Retirar"
        peligro
        cargando={retirar.isPending}
        onCancelar={() => setDialogo(null)}
        onConfirmar={() => void ejecutar(() => retirar.mutateAsync(), 'Postulación retirada.')}
      />
      <ConfirmDialog
        abierto={dialogo === 'AceptarOferta'}
        titulo="Aceptar oferta"
        mensaje={`Al aceptar la oferta de «${titulo}», sus demás postulaciones activas se retirarán y no podrá postularse a otras vacantes.`}
        textoConfirmar="Aceptar oferta"
        cargando={responderOferta.isPending}
        onCancelar={() => setDialogo(null)}
        onConfirmar={() => void ejecutar(() => responderOferta.mutateAsync(true), 'Oferta aceptada.')}
      />
      <ConfirmDialog
        abierto={dialogo === 'DeclinarOferta'}
        titulo="Declinar oferta"
        mensaje={`¿Declinar la oferta de «${titulo}»? Esta acción no se puede deshacer.`}
        textoConfirmar="Declinar oferta"
        peligro
        cargando={responderOferta.isPending}
        onCancelar={() => setDialogo(null)}
        onConfirmar={() => void ejecutar(() => responderOferta.mutateAsync(false), 'Oferta declinada.')}
      />
      <SeleccionarCurriculumDialog
        abierto={dialogo === 'Reactivar'}
        titulo="Reactivar postulación"
        descripcion="Puede reenviarla con el mismo currículum u otro. Se validan de nuevo la vacante y su perfil."
        textoConfirmar="Reactivar"
        enviar={(curriculumId) => reactivar.mutateAsync(curriculumId)}
        onCerrar={() => setDialogo(null)}
        onExito={() => notificar.exito('Postulación reactivada.')}
      />
    </>
  )
}
