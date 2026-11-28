import type { ItemMenu } from '@/shared/navegacion/menu'

export const menu: ItemMenu[] = [
  {
    etiqueta: 'Vacantes',
    ruta: '/estudiante/vacantes',
    roles: ['Estudiante'],
    descripcion: 'Explore las vacantes publicadas y postúlese.',
    orden: 10,
  },
  {
    etiqueta: 'Mis postulaciones',
    ruta: '/estudiante/postulaciones',
    roles: ['Estudiante'],
    descripcion: 'Siga el estado de sus postulaciones y responda a las ofertas.',
    orden: 20,
  },
  {
    etiqueta: 'Postulantes',
    ruta: '/empresa/postulantes',
    roles: ['AdminEmpresa', 'Reclutador'],
    descripcion: 'Revise a quienes se postularon a sus vacantes y avance su proceso.',
    orden: 30,
  },
]
