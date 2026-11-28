import { z } from 'zod'

// Mismos textos que docs/mensajes-validacion.md y los validadores de Application/Features/Postulaciones.

export const esquemaSeleccionCurriculum = z.object({
  curriculumId: z.string().min(1, 'Seleccione un currículum.'),
})
export type SeleccionCurriculumFormulario = z.infer<typeof esquemaSeleccionCurriculum>

export const esquemaCambioEstado = z.object({
  nuevoEstado: z.string().min(1, 'Seleccione el nuevo estado.'),
  comentario: z.string().max(500, 'Máximo 500 caracteres.'),
})
export type CambioEstadoFormulario = z.infer<typeof esquemaCambioEstado>
