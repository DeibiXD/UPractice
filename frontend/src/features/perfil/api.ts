import { http } from '@/shared/api/http'
import type {
  ActualizarHabilidadesRequest,
  ActualizarPerfilAcademicoRequest,
  CarreraDto,
  Guid,
  HabilidadDto,
  PerfilEstudianteDto,
  PrivacidadDto,
} from '@/shared/api/types'

/** HU-02 (S5.5). */
export const perfilApi = {
  obtener: async () => (await http.get<PerfilEstudianteDto>('/estudiantes/me/perfil')).data,
  actualizarAcademico: async (datos: ActualizarPerfilAcademicoRequest) =>
    (await http.put<PerfilEstudianteDto>('/estudiantes/me/perfil/academico', datos)).data,
  actualizarHabilidades: async (datos: ActualizarHabilidadesRequest) =>
    (await http.put<PerfilEstudianteDto>('/estudiantes/me/habilidades', datos)).data,
  actualizarPrivacidad: async (datos: PrivacidadDto) => {
    await http.put('/estudiantes/me/privacidad', datos)
  },
  carreras: async (universidadId: Guid) =>
    (await http.get<CarreraDto[]>('/catalogos/carreras', { params: { universidadId } })).data,
  habilidades: async () => (await http.get<HabilidadDto[]>('/catalogos/habilidades')).data,
}
