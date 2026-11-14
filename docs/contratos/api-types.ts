// Contratos de la API de UPractice (§5.6 de docs/fase-1/UPractice-Fase1.md, ERD v4).
// El frontend los copia en frontend/src/shared/api/types.ts.
// Si se cambia un DTO, se cambia aquí, en el diseño (§5) y en el record de C# en el mismo PR.

// ===== Comunes =====
export type Guid = string;
export type FechaISO = string;   // "2026-11-05T22:00:00Z"
export type FechaSolo = string;  // "2026-12-15"

export type Rol = 'SuperAdmin' | 'Estudiante' | 'Reclutador' | 'AdminEmpresa' | 'Coordinador';
export type Modalidad = 'Presencial' | 'Remota' | 'Hibrida';
export type EstadoVacante = 'Borrador' | 'Publicada' | 'Pausada' | 'Cerrada';
export type EstadoPostulacion =
  | 'Enviada' | 'EnRevision' | 'Entrevista' | 'Oferta'
  | 'Aceptada' | 'Declinada' | 'Rechazada' | 'Retirada';
export type EstadoHabilitacion = 'Pendiente' | 'Aprobada' | 'Rechazada';
export type EstadoVerificacion = 'Pendiente' | 'Verificado' | 'Rechazado';

export interface PaginaResultado<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
}
export interface Referencia { id: Guid; nombre: string; }
export interface UrlDescargaDto { url: string; expiraEn: FechaISO; }

// ===== Auth =====
export interface RegistroEstudianteRequest {
  nombre1: string;
  nombre2?: string | null;
  apellido1: string;
  apellido2?: string | null;
  correo: string;
  password: string;
  confirmarPassword: string;
}
export interface RegistroEmpresaRequest {
  nombreComercial: string;
  razonSocial: string;
  rtn: string;
  sector: string;
  sitioWeb?: string;
  universidadesIds: Guid[];
  admin: {
    nombre1: string;
    nombre2?: string | null;
    apellido1: string;
    apellido2?: string | null;
    correo: string;
    password: string;
    confirmarPassword: string;
  };
}
export interface LoginRequest { correo: string; password: string; }
export interface LoginResponse { accessToken: string; expiraEn: FechaISO; usuario: UsuarioActualDto; }
export interface UsuarioActualDto {
  id: Guid;
  nombreCompleto: string;   // calculado a partir de nombres y apellidos
  correo: string;
  rol: Rol;
  empresa: Referencia | null;
  universidad: Referencia | null;
  debeCambiarPassword: boolean;   // true → el frontend redirige a /cambiar-password
}
export interface CambiarPasswordRequest {
  passwordActual: string;
  passwordNuevo: string;
  confirmarPassword: string;
}

// ===== Catálogos =====
export interface UniversidadDto { id: Guid; nombre: string; siglas: string; dominioCorreo: string; }
export interface CarreraDto { id: Guid; nombre: string; facultad: string; universidadId: Guid; }
export interface HabilidadDto { id: Guid; nombre: string; }

// ===== Perfil del estudiante =====
export interface PrivacidadDto { mostrarIndice: boolean; }
export interface PerfilEstudianteDto {
  usuarioId: Guid;
  nombreCompleto: string;
  correo: string;
  universidad: Referencia;
  numeroCuenta: string | null;
  carrera: CarreraDto | null;
  indiceAcademico: number | null;
  habilidades: HabilidadDto[];
  estadoVerificacion: EstadoVerificacion;
  observacionVerificacion: string | null;
  fechaVerificacion: FechaISO | null;
  privacidad: PrivacidadDto;
}
export interface ActualizarPerfilAcademicoRequest {   // solo si estado ≠ Verificado
  numeroCuenta: string;
  carreraId: Guid;
  indiceAcademico: number;
}
export interface ActualizarHabilidadesRequest {
  habilidadesIds: Guid[];
}

// ===== Currículums =====
export interface CurriculumDto {
  id: Guid;
  nombreVersion: string;
  esPrincipal: boolean;
}
// POST multipart/form-data: archivo (File, PDF ≤ 5 MB), nombreVersion (string)

// ===== Vacantes =====
export interface FiltroVacantesQuery {
  busqueda?: string;
  carreraId?: Guid;
  modalidad?: Modalidad;
  remunerada?: boolean;
  pagina?: number;
  tamanoPagina?: number;
}
export interface VacanteResumenDto {
  id: Guid;
  titulo: string;
  empresa: Referencia;
  modalidad: Modalidad;
  ubicacion: string;
  remunerada: boolean;
  carreras: string[];
  fechaPublicacion: FechaISO;
  fechaCierre: FechaSolo;
  cumploRequisitos: boolean;   // calculado para el estudiante actual
  yaPostulado: boolean;
}
export interface HabilidadVacanteDto { id: Guid; nombre: string; esRequerida: boolean; }
export interface VacanteDetalleDto extends VacanteResumenDto {
  descripcion: string;
  cupos: number;
  carrerasDetalle: CarreraDto[];
  indiceMinimo: number | null;
  habilidades: HabilidadVacanteDto[];
}
export interface CrearVacanteRequest {
  titulo: string;
  descripcion: string;
  modalidad: Modalidad;
  ubicacion: string;
  cupos: number;
  remunerada: boolean;
  indiceMinimo: number | null;
  fechaCierre: FechaSolo;
  carrerasIds: Guid[];
  habilidades: { habilidadId: Guid; esRequerida: boolean }[];
  publicar: boolean;            // false = Borrador
}
export type ActualizarVacanteRequest = Omit<CrearVacanteRequest, 'publicar'>;
export interface CambiarEstadoVacanteRequest { nuevoEstado: 'Publicada' | 'Pausada' | 'Cerrada'; }
export interface VacanteEmpresaDto {
  id: Guid;
  titulo: string;
  descripcion: string;
  modalidad: Modalidad;
  ubicacion: string;
  cupos: number;
  remunerada: boolean;
  indiceMinimo: number | null;
  fechaCierre: FechaSolo;
  fechaPublicacion: FechaISO | null;
  estado: EstadoVacante;
  carreras: CarreraDto[];
  habilidades: HabilidadVacanteDto[];
  creadaPor: string;
  totalPostulaciones: number;
  postulacionesPorEstado: Partial<Record<EstadoPostulacion, number>>;
}

// ===== Postulaciones (estudiante) =====
export type AccionPostulacion = 'Retirar' | 'Reactivar' | 'AceptarOferta' | 'DeclinarOferta';
export interface CrearPostulacionRequest { curriculumId: Guid; }
export type ReactivarPostulacionRequest = CrearPostulacionRequest;   // puede elegir otro CV, mismas validaciones
export interface MiPostulacionDto {
  id: Guid;
  vacante: { id: Guid; titulo: string };
  empresa: Referencia;
  estado: EstadoPostulacion;
  fechaPostulacion: FechaISO;
  fechaUltimoCambio: FechaISO;      // fecha del último registro del historial
  acciones: AccionPostulacion[];
}
export interface HistorialItemDto {
  estadoAnterior: EstadoPostulacion | null;
  estadoNuevo: EstadoPostulacion;
  fecha: FechaISO;
  comentario: string | null;
}
export interface MiPostulacionDetalleDto extends MiPostulacionDto {
  curriculum: { id: Guid; nombreVersion: string };
  historial: HistorialItemDto[];
}
export interface ResponderOfertaRequest { aceptar: boolean; }

// ===== Postulaciones (empresa) =====
export interface PostulanteResumenDto {
  postulacionId: Guid;
  estudiante: { nombreCompleto: string; universidad: string; carrera: string };
  estado: EstadoPostulacion;
  cumpleRequisitos: boolean;
  fechaPostulacion: FechaISO;
}
export interface PostulanteDetalleDto {
  postulacionId: Guid;
  vacante: { id: Guid; titulo: string };
  estado: EstadoPostulacion;
  cumpleRequisitos: boolean;
  fechaPostulacion: FechaISO;
  estudiante: {
    nombreCompleto: string;
    correo: string;
    universidad: string;
    facultad: string;
    carrera: string;
    indiceAcademico: number | null;   // null si no autorizó
    habilidades: string[];
    verificadoPor: string;            // nombre de la universidad
    fechaVerificacion: FechaISO;
  };
  curriculum: { id: Guid; nombreVersion: string };
  historial: (HistorialItemDto & { cambiadoPor: string })[];
}
export interface CambiarEstadoPostulacionRequest {
  nuevoEstado: 'EnRevision' | 'Entrevista' | 'Oferta' | 'Rechazada';
  comentario?: string | null;
}

// ===== Usuarios y habilitaciones de la empresa =====
export interface UsuarioEmpresaDto {
  id: Guid;
  nombreCompleto: string;
  correo: string;
  rol: 'Reclutador' | 'AdminEmpresa';
  activo: boolean;
}
export interface CrearUsuarioEmpresaRequest {   // la contraseña temporal la genera el sistema
  nombre1: string;
  nombre2?: string | null;
  apellido1: string;
  apellido2?: string | null;
  correo: string;
  rol: 'Reclutador' | 'AdminEmpresa';
}
export interface CambiarEstadoUsuarioRequest { activo: boolean; }
export interface HabilitacionDto {
  id: Guid;
  universidad: Referencia;
  estado: EstadoHabilitacion;
  motivoRechazo: string | null;
  fechaSolicitud: FechaISO;
  fechaResolucion: FechaISO | null;
}
export interface SolicitarHabilitacionRequest { universidadId: Guid; }

// ===== Coordinación =====
export interface SolicitudHabilitacionDto {
  id: Guid;
  empresa: {
    id: Guid;
    nombreComercial: string;
    razonSocial: string;
    rtn: string;
    sector: string;
    sitioWeb: string | null;
  };
  estado: EstadoHabilitacion;
  fechaSolicitud: FechaISO;
}
export interface ResolverHabilitacionRequest { aprobar: boolean; motivoRechazo?: string | null; }
export interface EstudianteVerificacionDto {
  usuarioId: Guid;
  nombreCompleto: string;
  correo: string;
  numeroCuenta: string;
  carrera: string;
  indiceAcademico: number;
  estadoVerificacion: EstadoVerificacion;
}
export interface ResolverVerificacionRequest { aprobar: boolean; observacion?: string | null; }

// ===== Administración (SuperAdmin) =====
export interface CambiarEstadoRequest { activo: boolean; }
export interface GuardarNombreRequest { nombre: string; }
export interface GuardarUniversidadRequest { nombre: string; siglas: string; dominioCorreo: string; }
export interface UniversidadAdminDto {
  id: Guid;
  nombre: string;
  siglas: string;
  dominioCorreo: string;
  activa: boolean;
  totalEstudiantes: number;
  totalCoordinadores: number;
}
export interface CarreraAdminDto { id: Guid; nombre: string; activa: boolean; }
export interface FacultadAdminDto { id: Guid; nombre: string; carreras: CarreraAdminDto[]; }
export interface CoordinadorAdminDto {
  id: Guid;
  nombreCompleto: string;
  correo: string;
  universidad: Referencia;
  activo: boolean;
}
export interface CrearCoordinadorRequest {
  universidadId: Guid;
  nombre1: string;
  nombre2?: string | null;
  apellido1: string;
  apellido2?: string | null;
  correo: string;
}
export interface CuentaCreadaDto {          // también lo devuelve POST /empresa/usuarios
  usuarioId: Guid;
  correo: string;
  passwordTemporal: string;          // se muestra una sola vez; no se puede volver a consultar
}
export interface EmpresaAdminDto {
  id: Guid;
  nombreComercial: string;
  razonSocial: string;
  rtn: string;
  sector: string;
  activa: boolean;
  motivoSuspension: string | null;
  creadoEn: FechaISO;
  habilitaciones: { universidad: string; estado: EstadoHabilitacion }[];
}
export interface CambiarEstadoEmpresaRequest { activa: boolean; motivoSuspension?: string | null; }
