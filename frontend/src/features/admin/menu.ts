import type { ItemMenu } from '@/shared/navegacion/menu'

export const menu: ItemMenu[] = [
  {
    etiqueta: 'Coordinadores',
    ruta: '/admin/coordinadores',
    roles: ['SuperAdmin'],
    descripcion: 'Cree, desactive y restablezca cuentas de coordinador.',
    orden: 20,
  },
  {
    etiqueta: 'Empresas',
    ruta: '/admin/empresas',
    roles: ['SuperAdmin'],
    descripcion: 'Supervise empresas y suspenda las que incumplan.',
    orden: 30,
  },
]
