import type { ItemMenu } from '@/shared/navegacion/menu'

export const menu: ItemMenu[] = [
  {
    etiqueta: 'Mis currículums',
    ruta: '/estudiante/curriculums',
    roles: ['Estudiante'],
    descripcion: 'Suba hasta 5 versiones de su CV y elija la principal.',
    orden: 60,
  },
]
