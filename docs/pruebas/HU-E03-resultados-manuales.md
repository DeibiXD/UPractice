# Registro de pruebas manuales de HU-E03

Estado: plantilla pendiente de completar con los casos ejecutados, las respuestas observadas y sus capturas. Este documento no sustituye el informe de pruebas automatizadas.

## Datos de la ejecución

- Fecha y hora: por completar.
- Persona que realizó las pruebas: por completar.
- Rama: `hu-e03-crear-publicar-vacantes`.
- Commit y cambios locales probados: por completar.
- Entorno: API local, Swagger y PostgreSQL en Docker.
- Usuario y rol: por completar; no incluir contraseña ni token.
- Guía utilizada: [HU-E03.md](HU-E03.md).

## Resultados

Completa el resultado observado con el código HTTP, los campos relevantes y el ID creado cuando corresponda. Cambia «Por registrar» por «Correcto», «Falló» o «No ejecutado» según lo comprobado. Los casos siguientes son sugeridos; no se consideran ejecutados por aparecer en la tabla.

| Caso | Acción | Resultado esperado | Resultado observado | Estado | Evidencia |
|---|---|---|---|---|---|
| M01 | Crear con `publicar=false` y carrera permitida | 201, estado `Borrador`, `fechaPublicacion=null` | Por completar | Por registrar | Por adjuntar |
| M02 | Crear con `publicar=true` y carrera permitida | 201, estado `Publicada`, fecha de publicación UTC | Por completar | Por registrar | Por adjuntar |
| M03 | Crear con una habilidad real y `esRequerida=true` | 201; respuesta y relación guardada conservan la habilidad y su condición | Por completar | Por registrar | Por adjuntar |
| M04 | Enviar título de un carácter | 400 con `errors.titulo` | Por completar | Por registrar | Por adjuntar |
| M05 | Enviar `cupos=0` | 400 con `errors.cupos` | Por completar | Por registrar | Por adjuntar |
| M06 | Enviar fecha de cierre igual a hoy de Honduras | 400 con `errors.fechaCierre` | Por completar | Por registrar | Por adjuntar |
| M07 | Ejecutar el POST sin token | 401 | Por completar | Por registrar | Por adjuntar |
| M08 | Consultar en DBeaver el ID devuelto en M01/M02 | Vacante persistida, estado correcto y empresa/autor del usuario autenticado | Por completar | Por registrar | Por adjuntar |

## Cómo guardar las capturas

1. Crea la carpeta `evidencias/HU-E03` dentro de `docs/pruebas`.
2. Captura el cuerpo enviado y la respuesta de Swagger, incluyendo el código HTTP. Para M08, captura la consulta por ID y su resultado en DBeaver.
3. Usa nombres como `M01-borrador.png`, `M02-publicada.png` y `M08-persistencia.png`.
4. En la columna Evidencia, sustituye «Por adjuntar» por el enlace correspondiente, por ejemplo: `[Captura](evidencias/HU-E03/M01-borrador.png)`.
5. Recorta u oculta tokens, cabeceras Authorization, contraseñas y hashes. Conserva los IDs de prueba necesarios para relacionar la respuesta y los registros.

Si no conservaste las respuestas originales, vuelve a ejecutar los casos que quieras documentar y registra la nueva fecha. Cada POST correcto crea otra vacante; la consulta de comprobación debe utilizar el ID de esa respuesta.

## Incidencias y observaciones

Por completar. Si un caso falla, registra la entrada, el resultado observado y el comportamiento esperado; conserva la evidencia del fallo. La revisión de este documento y las capturas se puede incluir en el PR del bloque backend.
