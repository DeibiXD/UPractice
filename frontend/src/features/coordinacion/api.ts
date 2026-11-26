import { http } from '@/shared/api/http'
import type {
  EstadoVerificacion,
  EstudianteVerificacionDto,
  Guid,
  PaginaResultado,
  ResolverVerificacionRequest,
} from '@/shared/api/types'

export interface FiltroVerificacion {
  estadoVerificacion?: EstadoVerificacion
  pagina: number
  tamanoPagina: number
}

/** HU-C02 (S5.5). Solo estudiantes de la universidad del coordinador. */
export const verificacionApi = {
  listar: async (filtro: FiltroVerificacion) =>
    (await http.get<PaginaResultado<EstudianteVerificacionDto>>('/coordinacion/estudiantes', { params: filtro })).data,
  resolver: async (usuarioId: Guid, datos: ResolverVerificacionRequest) => {
    await http.patch(`/coordinacion/estudiantes/${usuarioId}/verificacion`, datos)
  },
}
