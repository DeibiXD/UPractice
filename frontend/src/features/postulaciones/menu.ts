import type { ItemMenu } from '@/shared/navegacion/menu'

export const menu: ItemMenu[] = [
  {
    etiqueta: 'Mis postulaciones',
    ruta: '/estudiante/postulaciones',
    roles: ['Estudiante'],
    descripcion: 'Siga el estado de sus postulaciones y responda a las ofertas.',
    orden: 20,
  },
]
