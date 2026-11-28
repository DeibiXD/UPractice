import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type {
  CambiarEstadoPostulacionRequest,
  EstadoPostulacion,
  FiltroPostulantesQuery,
  FiltroVacantesQuery,
  Guid,
  MiPostulacionDetalleDto,
} from '@/shared/api/types'
import { postulacionesApi as api } from './api'

// Todas las claves de caché de esta feature empiezan por uno de estos prefijos.
const PREFIJOS = ['vacantes', 'mis-postulaciones', 'postulantes', 'vacantes-empresa'] as const

/** Después de cualquier cambio se invalidan las listas y detalles que pudieron cambiar (p. ej. `yaPostulado`). */
function useInvalidar() {
  const cliente = useQueryClient()
  return () => Promise.all(PREFIJOS.map((prefijo) => cliente.invalidateQueries({ queryKey: [prefijo] })))
}

// ----- Vacantes (estudiante) -----
export const useVacantes = (filtro: FiltroVacantesQuery) =>
  useQuery({ queryKey: ['vacantes', 'lista', filtro], queryFn: () => api.listarVacantes(filtro) })

export const useVacante = (id: Guid) =>
  useQuery({ queryKey: ['vacantes', 'detalle', id], queryFn: () => api.obtenerVacante(id) })

export const useMisCurriculums = (habilitado: boolean) =>
  useQuery({ queryKey: ['curriculums'], queryFn: () => api.listarMisCurriculums(), enabled: habilitado })

// ----- Postulaciones (estudiante) -----
export const useMisPostulaciones = (estado?: EstadoPostulacion) =>
  useQuery({
    queryKey: ['mis-postulaciones', 'lista', estado ?? 'todas'],
    queryFn: () => api.listarMias({ estado }),
  })

export const useMiPostulacion = (id: Guid) =>
  useQuery({ queryKey: ['mis-postulaciones', 'detalle', id], queryFn: () => api.obtenerMia(id) })

export function usePostular(vacanteId: Guid) {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (curriculumId: Guid) => api.postular(vacanteId, { curriculumId }),
    onSuccess: invalidar,
  })
}

/** Cada acción devuelve el detalle actualizado: se guarda en caché y se refrescan las listas. */
function useAccionSobreMiPostulacion<TVariables>(id: Guid, accion: (variables: TVariables) => Promise<MiPostulacionDetalleDto>) {
  const cliente = useQueryClient()
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: accion,
    onSuccess: async (detalle) => {
      cliente.setQueryData(['mis-postulaciones', 'detalle', id], detalle)
      await invalidar()
    },
  })
}

export const useRetirar = (id: Guid) => useAccionSobreMiPostulacion<void>(id, () => api.retirar(id))

export const useReactivar = (id: Guid) =>
  useAccionSobreMiPostulacion<Guid>(id, (curriculumId) => api.reactivar(id, { curriculumId }))

export const useResponderOferta = (id: Guid) =>
  useAccionSobreMiPostulacion<boolean>(id, (aceptar) => api.responderOferta(id, { aceptar }))

// ----- Postulantes (empresa) -----
export const useVacantesEmpresa = (pagina: number) =>
  useQuery({ queryKey: ['vacantes-empresa', 'lista', pagina], queryFn: () => api.listarVacantesEmpresa(pagina) })

export const useVacanteEmpresa = (id: Guid) =>
  useQuery({ queryKey: ['vacantes-empresa', 'detalle', id], queryFn: () => api.obtenerVacanteEmpresa(id) })

export const usePostulantes = (vacanteId: Guid, filtro: FiltroPostulantesQuery) =>
  useQuery({
    queryKey: ['postulantes', 'lista', vacanteId, filtro],
    queryFn: () => api.listarPostulantes(vacanteId, filtro),
  })

export const usePostulante = (id: Guid) =>
  useQuery({ queryKey: ['postulantes', 'detalle', id], queryFn: () => api.obtenerPostulante(id) })

export function useCambiarEstado(id: Guid) {
  const cliente = useQueryClient()
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (datos: CambiarEstadoPostulacionRequest) => api.cambiarEstado(id, datos),
    onSuccess: async (detalle) => {
      cliente.setQueryData(['postulantes', 'detalle', id], detalle)
      await invalidar()
    },
  })
}
