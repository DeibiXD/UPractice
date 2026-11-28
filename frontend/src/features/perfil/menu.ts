import type { ItemMenu } from '@/shared/navegacion/menu'

export const menu: ItemMenu[] = [
  {
    etiqueta: 'Mi perfil',
    ruta: '/estudiante/perfil',
    roles: ['Estudiante'],
    descripcion: 'Complete sus datos académicos, habilidades y privacidad.',
    orden: 50,
  },
]
