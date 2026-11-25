import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { ChipEstado } from './ChipEstado'
import { EncabezadoPagina } from './EncabezadoPagina'
import { EstadoVacio } from './EstadoVacio'
import { TablaDatos, type ColumnaTabla } from './TablaDatos'

interface Fila {
  id: string
  nombre: string
}
const columnas: ColumnaTabla<Fila>[] = [
  { id: 'nombre', encabezado: 'Nombre', render: (f) => f.nombre },
  { id: 'id', encabezado: 'Código', render: (f) => f.id, alinear: 'right' },
]
const filas: Fila[] = [
  { id: 'a1', nombre: 'Ana' },
  { id: 'b2', nombre: 'Beto' },
]
const idFila = (f: Fila) => f.id

describe('TablaDatos', () => {
  it('con datos muestra encabezados y filas', () => {
    const html = renderToStaticMarkup(<TablaDatos columnas={columnas} filas={filas} idFila={idFila} />)
    for (const texto of ['Nombre', 'Código', 'Ana', 'Beto', 'a1', 'b2']) expect(html).toContain(texto)
  })

  it('cargando muestra el indicador y no las filas', () => {
    const html = renderToStaticMarkup(<TablaDatos columnas={columnas} filas={filas} idFila={idFila} cargando />)
    expect(html).toContain('role="status"')
    expect(html).not.toContain('Ana')
  })

  it('con error muestra el mensaje y el botón Reintentar', () => {
    const html = renderToStaticMarkup(
      <TablaDatos columnas={columnas} filas={undefined} idFila={idFila} error="No se pudo cargar." onReintentar={() => {}} />,
    )
    expect(html).toContain('No se pudo cargar.')
    expect(html).toContain('Reintentar')
  })

  it('sin filas muestra el mensaje de vacío configurado', () => {
    const html = renderToStaticMarkup(
      <TablaDatos columnas={columnas} filas={[]} idFila={idFila} vacio={{ titulo: 'Aún no tiene postulaciones.' }} />,
    )
    expect(html).toContain('Aún no tiene postulaciones.')
    expect(html).not.toContain('<table')
  })

  it('con onFilaClick las filas son accesibles con teclado', () => {
    const html = renderToStaticMarkup(
      <TablaDatos columnas={columnas} filas={filas} idFila={idFila} onFilaClick={() => {}} />,
    )
    expect(html).toContain('tabindex="0"')
  })
})

describe('ChipEstado', () => {
  it('muestra el estado con texto legible', () => {
    expect(renderToStaticMarkup(<ChipEstado estado="EnRevision" />)).toContain('En revisión')
    expect(renderToStaticMarkup(<ChipEstado estado="Publicada" />)).toContain('Publicada')
  })

  it('un estado desconocido no rompe y se muestra tal cual', () => {
    expect(renderToStaticMarkup(<ChipEstado estado="Nuevo" />)).toContain('Nuevo')
  })
})

describe('EstadoVacio y EncabezadoPagina', () => {
  it('EstadoVacio muestra título, descripción y acción', () => {
    const html = renderToStaticMarkup(
      <EstadoVacio titulo="Sin vacantes" descripcion="Cree la primera." accion={<button>Crear</button>} />,
    )
    for (const texto of ['Sin vacantes', 'Cree la primera.', 'Crear']) expect(html).toContain(texto)
  })

  it('EncabezadoPagina muestra título, subtítulo y acciones', () => {
    const html = renderToStaticMarkup(
      <EncabezadoPagina titulo="Mis vacantes" subtitulo="Panel" acciones={<button>Nueva</button>} />,
    )
    for (const texto of ['Mis vacantes', 'Panel', 'Nueva']) expect(html).toContain(texto)
  })
})
