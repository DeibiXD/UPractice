import { http } from '@/shared/api/http'
import type {
  CambiarEstadoPostulacionRequest,
  CrearPostulacionRequest,
  CurriculumDto,
  FiltroMisPostulacionesQuery,
  FiltroPostulantesQuery,
  FiltroVacantesQuery,
  Guid,
  MiPostulacionDetalleDto,
  MiPostulacionDto,
  PaginaResultado,
  PostulanteDetalleDto,
  PostulanteResumenDto,
  ReactivarPostulacionRequest,
  ResponderOfertaRequest,
  UrlDescargaDto,
  VacanteDetalleDto,
  VacanteEmpresaDto,
  VacanteResumenDto,
} from '@/shared/api/types'

export const postulacionesApi = {
  // ----- Vacantes (estudiante) -----
  listarVacantes: async (filtro: FiltroVacantesQuery) =>
    (await http.get<PaginaResultado<VacanteResumenDto>>('/vacantes', { params: filtro })).data,

  obtenerVacante: async (id: Guid) => (await http.get<VacanteDetalleDto>(`/vacantes/${id}`)).data,

  /** Los currículums del estudiante (HU-03). Se usa para elegir con cuál postularse. */
  listarMisCurriculums: async () => (await http.get<CurriculumDto[]>('/estudiantes/me/curriculums')).data,

  // ----- Postulaciones (estudiante) -----
  postular: async (vacanteId: Guid, datos: CrearPostulacionRequest) =>
    (await http.post<MiPostulacionDetalleDto>(`/vacantes/${vacanteId}/postulaciones`, datos)).data,

  listarMias: async (filtro: FiltroMisPostulacionesQuery) =>
    (await http.get<MiPostulacionDto[]>('/estudiantes/me/postulaciones', { params: filtro })).data,

  obtenerMia: async (id: Guid) =>
    (await http.get<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}`)).data,

  retirar: async (id: Guid) =>
    (await http.post<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}/retirar`)).data,

  reactivar: async (id: Guid, datos: ReactivarPostulacionRequest) =>
    (await http.post<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}/reactivar`, datos)).data,

  responderOferta: async (id: Guid, datos: ResponderOfertaRequest) =>
    (await http.post<MiPostulacionDetalleDto>(`/estudiantes/me/postulaciones/${id}/respuesta-oferta`, datos)).data,

  // ----- Postulantes (empresa) -----
  listarVacantesEmpresa: async (pagina: number, tamanoPagina = 10) =>
    (await http.get<PaginaResultado<VacanteEmpresaDto>>('/empresa/vacantes', { params: { pagina, tamanoPagina } }))
      .data,

  obtenerVacanteEmpresa: async (id: Guid) => (await http.get<VacanteEmpresaDto>(`/empresa/vacantes/${id}`)).data,

  listarPostulantes: async (vacanteId: Guid, filtro: FiltroPostulantesQuery) =>
    (await http.get<PostulanteResumenDto[]>(`/empresa/vacantes/${vacanteId}/postulaciones`, { params: filtro })).data,

  obtenerPostulante: async (id: Guid) =>
    (await http.get<PostulanteDetalleDto>(`/empresa/postulaciones/${id}`)).data,

  cambiarEstado: async (id: Guid, datos: CambiarEstadoPostulacionRequest) =>
    (await http.patch<PostulanteDetalleDto>(`/empresa/postulaciones/${id}/estado`, datos)).data,

  /** URL firmada (5 minutos) del CV adjunto a la postulación. */
  urlCurriculum: async (id: Guid) =>
    (await http.get<UrlDescargaDto>(`/empresa/postulaciones/${id}/curriculum/descarga`)).data,
}
