import type { Rol } from '@/shared/api/types'

/**
 * Pantalla principal de cada rol después de iniciar sesión (S5.6 del plan).
 * Mientras una pantalla no exista, el rol aterriza en "/" (InicioPage: tarjetas con las
 * secciones del menú de su rol, ver src/app/menu.ts).
 * Quien entregue la primera pantalla de un rol cambia SU línea:
 *   Estudiante    → '/vacantes'                (HU-04)
 *   Reclutador    → '/empresa/vacantes'        (HU-E03/E04)
 *   AdminEmpresa  → '/empresa/vacantes'
 *   Coordinador   → '/coordinacion/empresas'   (HU-C01)
 *   SuperAdmin    → '/admin/universidades'     (HU-A01)
 */
const INICIO_POR_ROL: Record<Rol, string> = {
  Estudiante: '/',
  Reclutador: '/',
  AdminEmpresa: '/',
  Coordinador: '/',
  SuperAdmin: '/',
}

export function rutaInicio(rol: Rol): string {
  return INICIO_POR_ROL[rol]
}
