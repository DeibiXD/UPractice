# Probar la gestión de vacantes de empresa

Esta entrega cubre el backend de HU-E04: listado paginado, detalle, edición y cambio de estado. Se reutiliza la creación de HU-E03. Las pantallas y su validación Zod corresponden a la integración posterior del frontend.

## Preparación

Con Docker Desktop y `upractice-pg` encendidos, inicia o reinicia la API para cargar el código de la rama:

```powershell
cd C:\Proyectos\UPractice\backend
dotnet run --project .\src\UPractice.Api --launch-profile http
```

Abre `http://localhost:5294/swagger`, inicia sesión con `rrhh@digitales.example.com` y tu contraseña demo local. Copia solo `accessToken` en Authorize. También puede operar un AdminEmpresa. Las contraseñas temporales deben cambiarse antes de usar estas rutas.

Si necesitas una vacante nueva, crea un borrador siguiendo [HU-E03](HU-E03.md). Cada POST exitoso crea una vacante diferente. Los GET y PATCH de esta guía utilizan el ID de la vacante que quieres administrar.

## Listar y consultar

En Swagger ejecuta `GET /api/v1/empresa/vacantes`. Los parámetros opcionales son:

| Parámetro | Valores |
|---|---|
| `estado` | Borrador, Publicada, Pausada o Cerrada; omitir para consultar todos. |
| `pagina` | Desde 1; predeterminado 1. |
| `tamanoPagina` | Entre 1 y 50; predeterminado 10. |

Ejemplo: `/api/v1/empresa/vacantes?estado=Borrador&pagina=1&tamanoPagina=10`.

La respuesta 200 contiene `items`, `pagina`, `tamanoPagina`, `totalRegistros` y `totalPaginas`. El orden es estable por ID descendente, generado como UUIDv7. Una página posterior a los resultados devuelve `items=[]` conservando los totales. El filtro se aplica antes de contar y paginar.

Copia un `items[].id` y ejecuta `GET /api/v1/empresa/vacantes/{id}`. La respuesta contiene los campos de la vacante, carreras, habilidades, creador, `totalPostulaciones` y `postulacionesPorEstado`. Los conteos incluyen los estados realmente presentes; una vacante sin postulaciones tiene total cero y un objeto de conteos vacío. Las carreras históricas siguen apareciendo aunque posteriormente se desactiven.

## Editar un borrador o una vacante pausada

Ejecuta `PUT /api/v1/empresa/vacantes/{id}` con el mismo contrato de creación, omitiendo `publicar`. Sustituye el ID de carrera por uno de una universidad aprobada para tu empresa y usa una fecha posterior a hoy de Honduras:

```json
{
  "titulo": "Practicante de análisis de datos",
  "descripcion": "Apoyo al equipo en el análisis de datos y la preparación de informes de seguimiento.",
  "modalidad": "Remota",
  "ubicacion": "San Pedro Sula",
  "cupos": 4,
  "remunerada": false,
  "indiceMinimo": 80,
  "fechaCierre": "2026-12-31",
  "carrerasIds": ["<ID real de una carrera permitida>"],
  "habilidades": []
}
```

Resultado esperado: 200 con los datos persistidos. El PUT reemplaza los campos y las selecciones; en este ejemplo se eliminan las habilidades previas al enviar la lista vacía. Conserva la empresa, el creador, el estado y la fecha de publicación. Repetir el mismo PUT no duplica los puentes de carreras/habilidades.

Solo se permite editar Borrador o Pausada. Una Publicada o Cerrada devuelve 422 `TRANSICION_INVALIDA`. Las selecciones se validan otra vez: las carreras deben estar activas, su universidad activa y aprobada para la empresa; los IDs inexistentes/duplicados devuelven 400 por campo. Una validación fallida conserva los datos anteriores.

## Publicar, pausar, reanudar y cerrar

Ejecuta `PATCH /api/v1/empresa/vacantes/{id}/estado` con:

```json
{
  "nuevoEstado": "Publicada"
}
```

El resultado correcto es 204, sin cuerpo. Consulta nuevamente el detalle para comprobar el cambio. La máquina permite:

| Estado de origen | Destino permitido |
|---|---|
| Borrador | Publicada |
| Publicada | Pausada o Cerrada |
| Pausada | Publicada o Cerrada |
| Cerrada | Ninguno |

Publicar y reanudar comprueban de nuevo fecha vigente, habilitación Aprobada y carreras válidas de universidades permitidas. La primera publicación asigna fecha UTC; reanudar conserva la fecha inicial. Una transición no permitida devuelve 422 `TRANSICION_INVALIDA`. Un estado omitido o desconocido devuelve 400.

## Fecha de cierre y estado efectivo

- Hoy se calcula mediante `America/Tegucigalpa`. Una vacante continúa vigente durante su fecha de cierre; vence cuando `fechaCierre < hoy`.
- Conforme al diseño §3.6, una **Publicada** vencida se presenta como **Cerrada**. El mismo estado efectivo se usa en detalle, filtro, totales/paginación y transiciones; no puede pausarse para reabrirla.
- Consultar no escribe el cierre en la BD. El job que materialice ese estado es opcional y no forma parte de esta entrega.
- Borrador y Pausada conservan su estado aunque haya pasado la fecha, permitiendo corregirla por PUT. Una Pausada vencida no puede reanudarse hasta corregir el cierre.
- Crear/editar exige una fecha nueva posterior a hoy; publicar/reanudar una vacante existente admite su propio día de cierre, coherente con las reglas de postulaciones.

## Permisos y aislamiento

1. Con el reclutador de Soluciones Digitales, consulta y modifica una vacante propia.
2. Inicia sesión con el reclutador de otra empresa y utiliza el mismo ID en GET, PUT y PATCH: debe devolver 404, igual que un ID inexistente.
3. El listado de la segunda empresa debe mostrar únicamente sus vacantes.
4. Sin token se obtiene 401. Estudiante, Coordinador o SuperAdmin reciben 403 en estas rutas. Una contraseña temporal pendiente produce 403 `CAMBIO_PASSWORD_REQUERIDO`.

La prueba automatizada utiliza empresas, estudiantes, CV y postulaciones propios para comprobar aislamiento y conteos, y limpia sus registros. No modifica el seed compartido ni la base de desarrollo.

## Verificación automatizada

Con Docker disponible, desde `backend`:

```powershell
dotnet build UPractice.slnx
dotnet test .\tests\UPractice.Tests\UPractice.Tests.csproj
```

Verificación del 21 de noviembre de 2026: **182 pruebas correctas, cero fallidas y cero omitidas** (113 existentes y 69 nuevas). La compilación terminó sin errores; hubo cuatro advertencias NU1900 porque no se pudo consultar el servicio de vulnerabilidades de NuGet. No se desactivó esa comprobación ni se cambiaron paquetes.

Las pruebas verifican HTTP, errores, OpenAPI, persistencia, paginación y estado efectivo, límites de fecha y aislamiento de lectura/escritura. El cierre completo de la historia mantiene pendientes frontend/Zod, revisión, fusión y despliegue.
