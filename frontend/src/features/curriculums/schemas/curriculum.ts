import { z } from 'zod'

// Mismos textos y reglas que backend/.../Features/Curriculums (MensajesCurriculums, SubirCurriculumValidator).
// El backend además revisa que el contenido empiece con "%PDF"; aquí basta extensión, tipo y tamaño.
export const MENSAJE_NOMBRE_VERSION = 'El nombre de la versión debe tener entre 3 y 60 caracteres.'
export const MENSAJE_ARCHIVO = 'Suba un archivo PDF de máximo 5 MB.'
export const TAMANO_MAXIMO = 5 * 1024 * 1024
export const MAXIMO_ACTIVOS = 5

const esPdf = (archivo: File) =>
  archivo.size > 0 &&
  archivo.size <= TAMANO_MAXIMO &&
  archivo.name.toLowerCase().endsWith('.pdf') &&
  archivo.type === 'application/pdf'

export const esquemaSubirCurriculum = z.object({
  nombreVersion: z.string().trim().min(3, MENSAJE_NOMBRE_VERSION).max(60, MENSAJE_NOMBRE_VERSION),
  archivo: z.instanceof(File, { message: MENSAJE_ARCHIVO }).refine(esPdf, MENSAJE_ARCHIVO),
})
export type SubirCurriculumFormulario = z.infer<typeof esquemaSubirCurriculum>
