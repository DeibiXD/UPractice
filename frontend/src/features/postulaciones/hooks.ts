import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type {
  CrearPostulacionRequest,
  EstadoPostulacion,
  Guid,
  MiPostulacionDetalleDto,
  ReactivarPostulacionRequest,
} from '@/shared/api/types'
import { curriculumsApi, postulacionesApi } from './api'

export const claves = {
  todas: ['postulaciones'] as const,
  lista: (estado?: EstadoPostulacion) => ['postulaciones', 'mias', estado ?? 'todas'] as const,
  detalle: (id: Guid) => ['postulaciones', 'detalle', id] as const,
  curriculums: ['curriculums', 'mios'] as const,
}

export function useMisPostulaciones(estado?: EstadoPostulacion) {
  return useQuery({ queryKey: claves.lista(estado), queryFn: () => postulacionesApi.listarMias(estado) })
}

export function useMiPostulacion(id: Guid) {
  return useQuery({ queryKey: claves.detalle(id), queryFn: () => postulacionesApi.detalle(id) })
}

export function useMisCurriculums() {
  return useQuery({ queryKey: claves.curriculums, queryFn: curriculumsApi.listar })
}

/** Guarda el detalle devuelto por la API y marca los listados como desactualizados. */
function useAlActualizar() {
  const cliente = useQueryClient()
  return (detalle: MiPostulacionDetalleDto) => {
    cliente.setQueryData(claves.detalle(detalle.id), detalle)
    void cliente.invalidateQueries({ queryKey: ['postulaciones', 'mias'] })
  }
}

export function usePostular(vacanteId: Guid) {
  const alActualizar = useAlActualizar()
  return useMutation({
    mutationFn: (datos: CrearPostulacionRequest) => postulacionesApi.postular(vacanteId, datos),
    onSuccess: alActualizar,
  })
}

export function useRetirar(id: Guid) {
  const alActualizar = useAlActualizar()
  return useMutation({ mutationFn: () => postulacionesApi.retirar(id), onSuccess: alActualizar })
}

export function useReactivar(id: Guid) {
  const alActualizar = useAlActualizar()
  return useMutation({
    mutationFn: (datos: ReactivarPostulacionRequest) => postulacionesApi.reactivar(id, datos),
    onSuccess: alActualizar,
  })
}

export function useResponderOferta(id: Guid) {
  const alActualizar = useAlActualizar()
  return useMutation({
    mutationFn: (aceptar: boolean) => postulacionesApi.responderOferta(id, { aceptar }),
    onSuccess: alActualizar,
  })
}
