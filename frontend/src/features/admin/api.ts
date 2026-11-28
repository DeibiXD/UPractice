import { http } from '@/shared/api/http'
import type {
  CambiarEstadoEmpresaRequest,
  CoordinadorAdminDto,
  CrearCoordinadorRequest,
  CuentaCreadaDto,
  EmpresaAdminDto,
  Guid,
  PaginaResultado,
  UniversidadDto,
} from '@/shared/api/types'

export interface FiltroEmpresas {
  busqueda?: string
  activa?: boolean
  pagina: number
  tamanoPagina: number
}

/** HU-A03 y HU-A04 (S5.5). Solo SuperAdmin. */
export const adminApi = {
  universidades: async () => (await http.get<UniversidadDto[]>('/catalogos/universidades')).data,

  coordinadores: async (universidadId?: Guid) =>
    (await http.get<CoordinadorAdminDto[]>('/admin/coordinadores', { params: { universidadId } })).data,
  crearCoordinador: async (datos: CrearCoordinadorRequest) =>
    (await http.post<CuentaCreadaDto>('/admin/coordinadores', datos)).data,
  cambiarEstadoCoordinador: async (id: Guid, activo: boolean) => {
    await http.patch(`/admin/coordinadores/${id}/estado`, { activo })
  },
  restablecerPassword: async (id: Guid) =>
    (await http.post<CuentaCreadaDto>(`/admin/coordinadores/${id}/restablecer-password`)).data,

  empresas: async (filtro: FiltroEmpresas) =>
    (await http.get<PaginaResultado<EmpresaAdminDto>>('/admin/empresas', { params: filtro })).data,
  cambiarEstadoEmpresa: async (id: Guid, datos: CambiarEstadoEmpresaRequest) => {
    await http.patch(`/admin/empresas/${id}/estado`, datos)
  },
}
