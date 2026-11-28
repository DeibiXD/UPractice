import { http } from '@/shared/api/http'
import type {
  CrearPostulacionRequest,
  CurriculumDto,
  EstadoPostulacion,
  Guid,
  MiPostulacionDetalleDto,
  MiPostulacionDto,
  ReactivarPostulacionRequest,
  ResponderOfertaRequest,
} from '@/shared/api/types'

/** Rutas de S5.5 (estudiante). Las acciones devuelven el detalle actualizado (200). */
export const postulacionesApi = {
  listarMias: async (estado?: EstadoPostulacion): Promise<MiPostulacionDto[]> =>
    (await http.get<MiPostulacionDto[]>('/estudiantes/me/postulaciones', { params: estado ? { estado } : undefined }))
      .data,

  detalle: async (id: Guid): Promise<MiPostulacionDetalleDto> =>
    (await http.get<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}`)).data,

  postular: async (vacanteId: Guid, datos: CrearPostulacionRequest): Promise<MiPostulacionDetalleDto> =>
    (await http.post<MiPostulacionDetalleDto>(`/vacantes/${vacanteId}/postulaciones`, datos)).data,

  retirar: async (id: Guid): Promise<MiPostulacionDetalleDto> =>
    (await http.post<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}/retirar`)).data,

  reactivar: async (id: Guid, datos: ReactivarPostulacionRequest): Promise<MiPostulacionDetalleDto> =>
    (await http.post<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}/reactivar`, datos)).data,

  responderOferta: async (id: Guid, datos: ResponderOfertaRequest): Promise<MiPostulacionDetalleDto> =>
    (await http.post<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}/respuesta-oferta`, datos)).data,
}

/** HU-03 (otro paquete): hasta que exista, la lista llega vacía o con error y el selector lo explica. */
export const curriculumsApi = {
  listar: async (): Promise<CurriculumDto[]> => (await http.get<CurriculumDto[]>('/estudiantes/me/curriculums')).data,
}
