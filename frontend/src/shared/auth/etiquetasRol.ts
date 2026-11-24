import type { Rol } from '@/shared/api/types'

const ETIQUETAS: Record<Rol, string> = {
  SuperAdmin: 'Administrador',
  Coordinador: 'Coordinador',
  AdminEmpresa: 'Administrador de empresa',
  Reclutador: 'Reclutador',
  Estudiante: 'Estudiante',
}

/** Nombre del rol para mostrar en pantalla. */
export function etiquetaRol(rol: Rol): string {
  return ETIQUETAS[rol]
}
