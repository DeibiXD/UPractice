import { beforeEach, describe, expect, it, vi } from 'vitest'
import { http } from '@/shared/api/http'
import { postulacionesApi as api } from './api'

vi.mock('@/shared/api/http', () => ({ http: { get: vi.fn(), post: vi.fn(), patch: vi.fn() } }))

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(http.get).mockResolvedValue({ data: 'g' })
  vi.mocked(http.post).mockResolvedValue({ data: 'p' })
  vi.mocked(http.patch).mockResolvedValue({ data: 'm' })
})

// Cada ruta y verbo se comprobó contra backend/src/UPractice.Api/Controllers (ver docs/contratos/api-types.ts).
describe('postulacionesApi: verbo, URL y cuerpo de cada llamada', () => {
  it('vacantes del estudiante', async () => {
    await api.listarVacantes({ busqueda: 'web', pagina: 2 })
    expect(http.get).toHaveBeenCalledWith('/vacantes', { params: { busqueda: 'web', pagina: 2 } })
    await api.obtenerVacante('v1')
    expect(http.get).toHaveBeenCalledWith('/vacantes/v1')
    await api.listarMisCurriculums()
    expect(http.get).toHaveBeenCalledWith('/estudiantes/me/curriculums')
  })

  it('postulaciones del estudiante', async () => {
    await api.postular('v1', { curriculumId: 'cv-1' })
    expect(http.post).toHaveBeenCalledWith('/vacantes/v1/postulaciones', { curriculumId: 'cv-1' })
    await api.listarMias({ estado: 'Oferta' })
    expect(http.get).toHaveBeenCalledWith('/estudiantes/me/postulaciones', { params: { estado: 'Oferta' } })
    await api.obtenerMia('p1')
    expect(http.get).toHaveBeenCalledWith('/estudiantes/me/postulaciones/p1')
    await api.retirar('p1')
    expect(http.post).toHaveBeenCalledWith('/estudiantes/me/postulaciones/p1/retirar')
    await api.reactivar('p1', { curriculumId: 'cv-2' })
    expect(http.post).toHaveBeenCalledWith('/estudiantes/me/postulaciones/p1/reactivar', { curriculumId: 'cv-2' })
    await api.responderOferta('p1', { aceptar: false })
    expect(http.post).toHaveBeenCalledWith('/estudiantes/me/postulaciones/p1/respuesta-oferta', { aceptar: false })
  })

  it('postulantes de la empresa', async () => {
    await api.listarVacantesEmpresa(3)
    expect(http.get).toHaveBeenCalledWith('/empresa/vacantes', { params: { pagina: 3, tamanoPagina: 10 } })
    await api.obtenerVacanteEmpresa('v1')
    expect(http.get).toHaveBeenCalledWith('/empresa/vacantes/v1')
    await api.listarPostulantes('v1', { estado: 'Enviada', cumpleRequisitos: true })
    expect(http.get).toHaveBeenCalledWith('/empresa/vacantes/v1/postulaciones', {
      params: { estado: 'Enviada', cumpleRequisitos: true },
    })
    await api.obtenerPostulante('p1')
    expect(http.get).toHaveBeenCalledWith('/empresa/postulaciones/p1')
    await api.cambiarEstado('p1', { nuevoEstado: 'Entrevista', comentario: null })
    expect(http.patch).toHaveBeenCalledWith('/empresa/postulaciones/p1/estado', { nuevoEstado: 'Entrevista', comentario: null })
    await api.urlCurriculum('p1')
    expect(http.get).toHaveBeenCalledWith('/empresa/postulaciones/p1/curriculum/descarga')
  })

  it('devuelve solo el cuerpo de la respuesta', async () => {
    expect(await api.obtenerVacante('v1')).toBe('g')
    expect(await api.retirar('p1')).toBe('p')
    expect(await api.cambiarEstado('p1', { nuevoEstado: 'Oferta', comentario: null })).toBe('m')
  })
})
