import { menu as menuCoordinacion } from '@/features/coordinacion'
import { menu as menuCurriculums } from '@/features/curriculums'
import { menu as menuPerfil } from '@/features/perfil'
import type { ItemMenu } from '@/shared/navegacion/menu'

/**
 * Menú de navegación. Igual que las rutas en router.tsx, solo COMPONE:
 *
 *   1. En la feature, declare sus entradas en `menu.ts` y expórtelas desde su `index.ts`:
 *        export const menu: ItemMenu[] = [
 *          { etiqueta: 'Vacantes', ruta: '/estudiante/vacantes', roles: ['Estudiante'],
 *            descripcion: 'Explore las vacantes y postúlese.', orden: 10 },
 *        ]
 *   2. Aquí: importe `menu as menuVacantes` desde '@/features/vacantes' y agréguelo con spread.
 *
 * "Inicio" siempre aparece primero y no se declara. El layout y la pantalla de inicio
 * muestran solo las entradas del rol del usuario.
 */
export const menu: ItemMenu[] = [
  ...menuPerfil,
  ...menuCurriculums,
  ...menuCoordinacion,
  // ...menuVacantes,
  // ...menuPostulaciones,
]
