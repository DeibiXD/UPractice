import type { Rol } from '@/shared/api/types'

/** Una entrada del menú de navegación. Cada feature declara las suyas (ver src/app/menu.ts). */
export interface ItemMenu {
  etiqueta: string
  ruta: string
  /** Roles que ven la entrada. */
  roles: Rol[]
  /** Texto corto que se muestra en la tarjeta de la pantalla de inicio. */
  descripcion?: string
  /** Menor número aparece primero (por defecto 100). Empates: orden de declaración. */
  orden?: number
}

/** Lo que Layout le entrega a las páginas hijas con <Outlet context>. */
export interface ContextoLayout {
  menuRol: ItemMenu[]
}

const ORDEN_POR_DEFECTO = 100

/** Entradas visibles para un rol, ordenadas. */
export function menuParaRol(items: readonly ItemMenu[], rol: Rol): ItemMenu[] {
  return items
    .map((item, indice) => ({ item, indice }))
    .filter(({ item }) => item.roles.includes(rol))
    .sort(
      (a, b) =>
        (a.item.orden ?? ORDEN_POR_DEFECTO) - (b.item.orden ?? ORDEN_POR_DEFECTO) || a.indice - b.indice,
    )
    .map(({ item }) => item)
}
