import { http } from '@/shared/api/http'
import type { CurriculumDto, Guid, UrlDescargaDto } from '@/shared/api/types'

/** HU-03 (S5.5). La subida es multipart: archivo (PDF ≤ 5 MB) y nombreVersion. */
export const curriculumsApi = {
  listar: async () => (await http.get<CurriculumDto[]>('/estudiantes/me/curriculums')).data,
  subir: async (nombreVersion: string, archivo: File) => {
    const formulario = new FormData()
    formulario.append('nombreVersion', nombreVersion)
    formulario.append('archivo', archivo)
    return (await http.post<CurriculumDto>('/estudiantes/me/curriculums', formulario)).data
  },
  marcarPrincipal: async (id: Guid) => {
    await http.patch(`/estudiantes/me/curriculums/${id}/principal`)
  },
  eliminar: async (id: Guid) => {
    await http.delete(`/estudiantes/me/curriculums/${id}`)
  },
  urlDescarga: async (id: Guid) => (await http.get<UrlDescargaDto>(`/estudiantes/me/curriculums/${id}/descarga`)).data,
}
