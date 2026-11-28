import type {
  CurriculumDto,
  MiPostulacionDetalleDto,
  PostulanteDetalleDto,
  PostulanteResumenDto,
  VacanteDetalleDto,
  VacanteEmpresaDto,
  VacanteResumenDto,
} from '@/shared/api/types'

export const CV_PRINCIPAL: CurriculumDto = { id: 'cv-1', nombreVersion: 'CV 2026', esPrincipal: true }
export const CV_SECUNDARIO: CurriculumDto = { id: 'cv-2', nombreVersion: 'CV en inglés', esPrincipal: false }

export function miPostulacion(cambios: Partial<MiPostulacionDetalleDto> = {}): MiPostulacionDetalleDto {
  return {
    id: 'p1',
    vacante: { id: 'v1', titulo: 'Desarrollador Web' },
    empresa: { id: 'e1', nombre: 'Digitales S.A.' },
    estado: 'Enviada',
    fechaPostulacion: '2026-11-20T15:00:00Z',
    fechaUltimoCambio: '2026-11-21T16:30:00Z',
    acciones: ['Retirar'],
    curriculum: { id: 'cv-1', nombreVersion: 'CV 2026' },
    historial: [
      { estadoAnterior: null, estadoNuevo: 'Enviada', fecha: '2026-11-20T15:00:00Z', comentario: null },
      { estadoAnterior: 'Enviada', estadoNuevo: 'EnRevision', fecha: '2026-11-21T16:30:00Z', comentario: 'Revisaremos su perfil.' },
    ],
    ...cambios,
  }
}

export function postulante(cambios: Partial<PostulanteDetalleDto> = {}): PostulanteDetalleDto {
  return {
    postulacionId: 'p1',
    vacante: { id: 'v1', titulo: 'Desarrollador Web' },
    estado: 'Enviada',
    cumpleRequisitos: true,
    fechaPostulacion: '2026-11-20T15:00:00Z',
    estudiante: {
      nombreCompleto: 'Ana López',
      correo: 'ana.lopez@unah.hn',
      universidad: 'UNAH',
      facultad: 'Ingeniería',
      carrera: 'Ingeniería en Sistemas',
      indiceAcademico: 85,
      habilidades: ['TypeScript', 'SQL'],
      verificadoPor: 'UNAH',
      fechaVerificacion: '2026-11-10T12:00:00Z',
    },
    curriculum: { id: 'cv-1', nombreVersion: 'CV 2026' },
    historial: [
      { estadoAnterior: null, estadoNuevo: 'Enviada', fecha: '2026-11-20T15:00:00Z', comentario: null, cambiadoPor: 'Ana López' },
    ],
    ...cambios,
  }
}

export function postulanteResumen(cambios: Partial<PostulanteResumenDto> = {}): PostulanteResumenDto {
  return {
    postulacionId: 'p1',
    estudiante: { nombreCompleto: 'Ana López', universidad: 'UNAH', carrera: 'Ingeniería en Sistemas' },
    estado: 'Enviada',
    cumpleRequisitos: true,
    fechaPostulacion: '2026-11-20T15:00:00Z',
    ...cambios,
  }
}

export function vacanteResumen(cambios: Partial<VacanteResumenDto> = {}): VacanteResumenDto {
  return {
    id: 'v1',
    titulo: 'Desarrollador Web',
    empresa: { id: 'e1', nombre: 'Digitales S.A.' },
    modalidad: 'Hibrida',
    ubicacion: 'Tegucigalpa',
    remunerada: true,
    carreras: ['Ingeniería en Sistemas'],
    fechaPublicacion: '2026-11-15T10:00:00Z',
    fechaCierre: '2026-12-15',
    cumploRequisitos: true,
    yaPostulado: false,
    ...cambios,
  }
}

export function vacanteDetalle(cambios: Partial<VacanteDetalleDto> = {}): VacanteDetalleDto {
  return {
    ...vacanteResumen(),
    descripcion: 'Desarrollo de aplicaciones web.\nTrabajo en equipo.',
    cupos: 2,
    carrerasDetalle: [],
    indiceMinimo: 80,
    habilidades: [
      { id: 'h1', nombre: 'TypeScript', esRequerida: true },
      { id: 'h2', nombre: 'Docker', esRequerida: false },
    ],
    ...cambios,
  }
}

export function vacanteEmpresa(cambios: Partial<VacanteEmpresaDto> = {}): VacanteEmpresaDto {
  return {
    id: 'v1',
    titulo: 'Desarrollador Web',
    descripcion: 'Desarrollo de aplicaciones web.',
    modalidad: 'Hibrida',
    ubicacion: 'Tegucigalpa',
    cupos: 2,
    remunerada: true,
    indiceMinimo: null,
    fechaCierre: '2026-12-15',
    fechaPublicacion: '2026-11-15T10:00:00Z',
    estado: 'Publicada',
    carreras: [],
    habilidades: [],
    creadaPor: 'Reclutador Demo',
    totalPostulaciones: 3,
    postulacionesPorEstado: { Enviada: 2, EnRevision: 1 },
    ...cambios,
  }
}
