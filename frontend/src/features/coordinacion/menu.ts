import type { ItemMenu } from '@/shared/navegacion/menu'

export const menu: ItemMenu[] = [
  {
    etiqueta: 'Verificar estudiantes',
    ruta: '/coordinacion/estudiantes',
    roles: ['Coordinador'],
    descripcion: 'Verifique o rechace los datos académicos de sus estudiantes.',
    orden: 20,
  },
]
