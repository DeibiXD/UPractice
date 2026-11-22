# Mensajes de validación (FluentValidation ↔ Zod)

> Fuente: S5.7 de `docs/fase-1/UPractice-Fase1.md`. Backend y frontend usan **el mismo texto, carácter por carácter**.
> Si cambia un mensaje, se cambia aquí, en el validador de C# y en el esquema Zod en el mismo PR.

## Reglas sintácticas (se validan en los dos lados)

| Campo | Regla | Mensaje |
|---|---|---|
| `nombre1`, `apellido1` | Requerido, 2–50, solo letras y espacios | "Debe tener entre 2 y 50 letras." |
| `nombre2`, `apellido2` | Opcional, máx. 50, solo letras y espacios | "Máximo 50 letras." |
| `correo` | Requerido, formato email, máx. 150 | "Ingrese un correo válido." |
| `password` / `passwordNuevo` | 8–64, al menos 1 mayúscula, 1 minúscula y 1 número | "La contraseña debe tener 8 a 64 caracteres, con mayúscula, minúscula y número." |
| `confirmarPassword` | Igual a `password` | "Las contraseñas no coinciden." |
| `password` (login) | Requerido | "Ingrese su contraseña." |
| `passwordActual` | Requerido | "Ingrese su contraseña actual." |
| `rtn` | `^\d{14}$` | "El RTN debe tener 14 dígitos." |
| `nombreComercial`, `razonSocial` | 2–150 | "Debe tener entre 2 y 150 caracteres." |
| `sector` | 2–80 | "Debe tener entre 2 y 80 caracteres." |
| `sitioWeb` | Opcional, URL `http(s)`, máx. 200 | "Ingrese una URL válida." |
| `universidadesIds` | Mínimo 1 | "Seleccione al menos una universidad." |
| `numeroCuenta` | `^\d{6,20}$` | "El número de cuenta debe tener solo dígitos (6 a 20)." |
| `indiceAcademico` | 0–100, máx. 2 decimales | "El índice debe estar entre 0 y 100." |
| `nombreVersion` | 3–60 | "El nombre de la versión debe tener entre 3 y 60 caracteres." |
| `archivo` (CV) | PDF, ≤ 5 MB | "Suba un archivo PDF de máximo 5 MB." |
| `titulo` | 5–120 | "El título debe tener entre 5 y 120 caracteres." |
| `descripcion` | 30–4000 | "La descripción debe tener entre 30 y 4000 caracteres." |
| `ubicacion` | 2–120 | "Debe tener entre 2 y 120 caracteres." |
| `modalidad` | Valor del enum | "Seleccione una modalidad." |
| `cupos` | Entero 1–50 | "Los cupos deben estar entre 1 y 50." |
| `indiceMinimo` | Opcional, 0–100 | "El índice mínimo debe estar entre 0 y 100." |
| `fechaCierre` | Posterior a hoy (hora de Honduras) | "La fecha de cierre debe ser posterior a hoy." |
| `carrerasIds` | Mínimo 1 | "Seleccione al menos una carrera." |
| `carrerasIds` (HU-E03/HU-E04) | IDs no vacíos | "Seleccione carreras válidas y activas." |
| `carrerasIds` (HU-E03/HU-E04) | Sin IDs repetidos | "No repita carreras." |
| `habilidades` (HU-E03/HU-E04) | Lista no nula, elementos e IDs no vacíos; puede estar vacía | "Seleccione habilidades válidas." |
| `habilidades` (HU-E03/HU-E04) | Sin IDs de habilidad repetidos | "No repita habilidades." |
| `estado`, `nuevoEstado` (HU-E04) | Estado de vacante reconocido | "Seleccione un estado de vacante válido." |
| `pagina` (HU-E04) | Entero mayor o igual a 1; predeterminado 1 | "La página debe ser mayor o igual a 1." |
| `tamanoPagina` (HU-E04) | Entero 1–50; predeterminado 10 | "El tamaño de página debe estar entre 1 y 50." |
| `comentario` (estado) | Opcional, máx. 500 | "Máximo 500 caracteres." |
| `motivoRechazo` | Obligatorio si `aprobar = false`, 10–500 | "Indique el motivo (10 a 500 caracteres)." |
| `observacion` (verificación) | Obligatoria si `aprobar = false`, 10–500 | "Indique la observación (10 a 500 caracteres)." |
| `nombre` (universidad, facultad, carrera, habilidad) | 2–150 | "Debe tener entre 2 y 150 caracteres." |
| `siglas` | `^[A-Z0-9]{2,15}$` | "Las siglas deben tener de 2 a 15 letras mayúsculas o dígitos." |
| `dominioCorreo` | Dominio válido sin `@`, máx. 100 (ej. `unah.hn`) | "Ingrese un dominio válido, sin @ (ej. unah.hn)." |
| `motivoSuspension` | Obligatorio si `activa = false`, 10–500 | "Indique el motivo (10 a 500 caracteres)." |

## Reglas semánticas (solo backend)

Necesitan la base de datos o el usuario actual, así que el frontend no las repite. Se devuelven como 409/422 con `code` (ver `backend/src/UPractice.Domain/Excepciones/CodigosError.cs`):

- Dominio de correo institucional → `CORREO_NO_INSTITUCIONAL`
- Unicidad de correo, RTN, postulación, siglas y dominio → `CORREO_EN_USO`, `RTN_EN_USO`, `POSTULACION_DUPLICADA`, `SIGLAS_EN_USO`, `DOMINIO_EN_USO`
- Carrera elegible, perfil verificado, empresa habilitada, carreras permitidas → `CARRERA_NO_ELEGIBLE`, `PERFIL_NO_VERIFICADO`, `EMPRESA_NO_HABILITADA`, `CARRERA_NO_PERMITIDA`
- Transiciones de estado, cupos y límite de CV → `TRANSICION_INVALIDA`, `SIN_CUPOS`, `LIMITE_CURRICULUMS`
- HU-E04: editar fuera de Borrador/Pausada → 422 `TRANSICION_INVALIDA`, "Solo se pueden editar vacantes en Borrador o Pausada."
- HU-E04: publicar/reanudar una vacante vencida → 422 `VACANTE_NO_DISPONIBLE`, "La vacante está vencida y no se puede publicar." Los saltos desde un estado efectivo Cerrada se rechazan primero mediante `TransicionesVacante` con `TRANSICION_INVALIDA`.

## Errores semánticos con campo (solo backend)

Se devuelven como 400 en `errors`, igual que la validación sintáctica, para que el formulario los pinte en el campo:

| Campo | Cuándo | Mensaje |
|---|---|---|
| `passwordActual` | La contraseña actual no coincide (`PUT /auth/password`) | "La contraseña actual no es correcta." |
| `passwordNuevo` | La nueva es igual a la actual | "La nueva contraseña debe ser distinta de la actual." |
| `carrerasIds` (HU-E03) | Carrera inexistente/inactiva o universidad inactiva | "Seleccione carreras válidas y activas." |
| `habilidades` (HU-E03) | Una habilidad no existe | "Seleccione habilidades válidas." |

En HU-E03, título, descripción y ubicación se validan y guardan sin espacios exteriores.
Los campos `modalidad`, `remunerada`, `publicar` y el indicador `esRequerida` de cada habilidad
deben enviarse explícitamente; su omisión
se reporta mediante el formato 400 de JSON/ModelState de la API.
El índice mínimo conserva el rango 0–100; la respuesta devuelve el valor persistido
con la precisión de la columna `numeric(5,2)`, igual que los timestamps guardados en UTC.

## Cómo se comprueba la simetría

1. En un formulario, quitar temporalmente el `zodResolver`.
2. Enviar datos inválidos.
3. El backend debe responder 400 con **el mismo mensaje en la misma llave camelCase**, y `aplicarErrores` lo pinta en el mismo campo.
