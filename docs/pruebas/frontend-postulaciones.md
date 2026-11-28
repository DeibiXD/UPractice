# Probar el frontend de postulaciones (HU-06, HU-E05, HU-E06, HU-E07)

Guía de prueba manual de la rama `feat/frontend-postulaciones`: botón de postular, «Mis postulaciones» (estudiante) y panel de postulantes (empresa). Complementa las pruebas automatizadas (`npm test`), que simulan la API; aquí se comprueba el flujo contra el backend real.

> Las vacantes del estudiante (lista y detalle) son **provisionales**. Si HU-04 entrega las suyas, se reemplazan y se conserva `BotonPostular`.

## Preparación

1. Mezcla `main` con la rama del backend que expone los currículums (`GET /estudiantes/me/curriculums` y la descarga del CV de la empresa). Sin ella, el selector de currículum y la vista del CV no funcionan.
2. Levanta el sistema: `docker compose up --build` (README, «Correr con Docker») o la API local más `npm run dev` en `frontend/` (puerto 5173).
3. Necesitas dos sesiones, en navegadores o perfiles distintos:
   - **Empresa:** `rrhh@digitales.example.com` (Reclutador) con tu `DEMO_PASSWORD`.
   - **Estudiante:** una cuenta de estudiante con al menos un currículum cargado y una vacante `Publicada` de la empresa anterior. El seed de `main` no incluye estudiantes; usa el de la rama del backend o créala con el registro.
4. No pegues contraseñas ni tokens en las evidencias.

## Casos del estudiante

| Caso | Acción | Resultado esperado | Resultado observado | Estado | Evidencia |
|---|---|---|---|---|---|
| E01 | Abrir **Vacantes** | Lista de vacantes publicadas; el filtro «Remunerada» (Sí/No) filtra bien | Por completar | Por registrar | Por adjuntar |
| E02 | Abrir una vacante y pulsar **Postular** | Diálogo con tus currículums; el principal viene preseleccionado | Por completar | Por registrar | Por adjuntar |
| E03 | Confirmar la postulación | Aviso «Postulación enviada.» y la vacante aparece en Mis postulaciones como `Enviada` | Por completar | Por registrar | Por adjuntar |
| E04 | Postular otra vez a la misma vacante | Error del backend visible, sin duplicar | Por completar | Por registrar | Por adjuntar |
| E05 | Probar con un estudiante sin currículums | Mensaje para subir uno; el botón de confirmar queda deshabilitado | Por completar | Por registrar | Por adjuntar |
| E06 | Abrir **Mis postulaciones** y filtrar por estado | Solo aparecen las del estado elegido | Por completar | Por registrar | Por adjuntar |
| E07 | Abrir el detalle de una postulación | Datos e historial; solo los botones que devuelve `acciones` | Por completar | Por registrar | Por adjuntar |
| E08 | **Retirar** (con confirmación) | Estado `Retirada`; aparece **Reactivar** | Por completar | Por registrar | Por adjuntar |
| E09 | **Reactivar** eligiendo currículum | Vuelve a un estado activo | Por completar | Por registrar | Por adjuntar |

## Casos de la empresa

| Caso | Acción | Resultado esperado | Resultado observado | Estado | Evidencia |
|---|---|---|---|---|---|
| P01 | Abrir **Postulantes** | Panel de vacantes con conteos por estado | Por completar | Por registrar | Por adjuntar |
| P02 | Entrar a los postulantes de una vacante y usar los filtros | La lista se filtra y pagina | Por completar | Por registrar | Por adjuntar |
| P03 | Abrir el detalle de la postulación de E03 | Datos del estudiante y CV visible por URL firmada | Por completar | Por registrar | Por adjuntar |
| P04 | Estudiante sin autorizar su índice | Se lee «No autorizado por el estudiante», nunca `0` | Por completar | Por registrar | Por adjuntar |
| P05 | **Cambiar estado** de `Enviada` | Solo ofrece En revisión, Entrevista y Rechazada (no Oferta) | Por completar | Por registrar | Por adjuntar |
| P06 | Cambiar a `Rechazada` con comentario de solo espacios | Se guarda sin comentario; aviso de que no se puede deshacer | Por completar | Por registrar | Por adjuntar |
| P07 | Pasar a `Oferta` y, en la sesión del estudiante, **Declinar** | Estado `Declinada` (no `Aceptada`) | Por completar | Por registrar | Por adjuntar |
| P08 | Repetir con **Aceptar oferta** | Estado `Aceptada` | Por completar | Por registrar | Por adjuntar |

## Menú y permisos

| Caso | Acción | Resultado esperado | Resultado observado | Estado | Evidencia |
|---|---|---|---|---|---|
| M01 | Iniciar sesión como estudiante | El menú muestra Vacantes y Mis postulaciones, no Postulantes | Por completar | Por registrar | Por adjuntar |
| M02 | Iniciar sesión como Reclutador/AdminEmpresa | El menú muestra Postulantes, no las del estudiante | Por completar | Por registrar | Por adjuntar |
| M03 | Como estudiante, abrir `/empresa/postulantes` a mano | No permite el acceso | Por completar | Por registrar | Por adjuntar |

## Registro

- Fecha y hora: por completar.
- Persona: por completar.
- Commit probado: por completar.
- Entorno: por completar.

## Incidencias y observaciones

Por completar.
