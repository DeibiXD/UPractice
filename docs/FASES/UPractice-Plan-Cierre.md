# UPractice — Plan de cierre (20 al 29 de noviembre)

> Hoy: **viernes 20 de noviembre de 2026**.
> **Fin del desarrollo y congelamiento: viernes 27 de noviembre.**
> **Pruebas y preparación: sábado 28 y domingo 29.** Presentación: semana del 30 de noviembre.
> Reemplaza el calendario de la sección 2 de `UPractice-Plan-Fases.md`; el resto de ese documento (cómo hacer Docker, despliegue, guion) sigue vigente.

---

## 1. Dónde estamos (revisión de `main` al 20 de noviembre)

### 1.1 Lo que ya está listo

| Lineamiento | Evidencia en el repo |
|---|---|
| Clean Architecture (Domain, Application, Infrastructure, Api) | `backend/src/*` (T-01, #38) |
| EF Core Code-First + `HasQueryFilter` por `EmpresaId` | `AppDbContext.Filtros.cs` (13 filtros), migración `Inicial`, `AislamientoTests` |
| JWT cifrado (JWE) con `EmpresaId` como claim | `GeneradorJwt.cs`, `ICurrentUser` (T-03) |
| FluentValidation + middleware global | `Middleware/`, `Filtros/`, `ErroresTests` (T-04) |
| Frontend base: rutas protegidas, Axios con Bearer, extractor de errores | `shared/auth/RutaProtegida.tsx`, `RutaPorRol.tsx`, `shared/api/http.ts`, `errores.ts` (T-05, #41) |
| Login y cambio de contraseña con Zod + RHF `onChange` | `features/auth` |
| Endpoint `/health` | `Program.cs` |
| Backend de HU-01, HU-E01, HU-A01 | #42 |
| Backend de HU-E03 (crear y publicar vacante) | #43 |
| Backend de HU-05, HU-06, HU-E05, HU-E06, HU-E07 (postulaciones) | #44 |

### 1.2 Lo que ya debía estar terminado y no lo está

| Debía estar en | Pendiente | Impacto en la nota |
|---|---|---|
| **H0 (mié 11)** | **T-06** Dockerfile multi-stage y `docker-compose.yml`: no existe ningún archivo. | **Fase 4 completa (15 %)** |
| **H0 (mié 11)** | **T-07** Supabase + Render + Vercel y **CI** (`.github/workflows/` no existe). | **Fase 5 completa (20 %)** |
| **H0 (mié 11)** | **T-08** almacenamiento: `AlmacenamientoServiceCollection` es un esqueleto vacío. Bloquea subir CV (HU-03). | Fase 2 |
| **H1 (mié 18)** | Frontend de registro (HU-01, HU-E01), perfil (HU-02), verificación (HU-C02), vacantes (HU-E03, HU-E04), postulación (HU-05). Solo existen las pantallas de login y cambio de contraseña. | **Fase 3 (25 %)** |
| **H1 (mié 18)** | Backend de HU-02 (perfil) y HU-C02 (verificación). | Fase 2 |
| Antes del frontend de postulaciones | Pendientes de la revisión del **PR #44** (ver 1.3). Ninguno se ha atendido en `main`. | Contratos (Fase 1) y Fase 2 |

Con el calendario original, Docker y el despliegue se montaban primero para que cada funcionalidad llegara sola a producción. **Fases 4 y 5 valen 35 % y hoy están en cero:** son la prioridad número uno de esta semana.

### 1.3 Pendientes de la revisión del PR #44

Revisión de LoadRon (16 nov), aprobada con estos puntos para un PR pequeño **antes de empezar el frontend de postulaciones**. Seguimiento en el issue **#45 (T-09)**, asignado a elaleph27:

| # | Tema | Acción | Estado en `main` |
|---|---|---|---|
| 1 | Nombres de rutas | Seguir el contrato S5.7: `/estudiantes/me/postulaciones`, `/estudiantes/me/postulaciones/{id}/retirar`, `POST .../respuesta-oferta`, `/empresa/vacantes/{id}/postulaciones`, `PATCH /empresa/postulaciones/{id}/estado` | Pendiente (siguen `/postulaciones`, `PUT .../oferta`, `PUT .../estado`) |
| 2 | Respuestas con detalle y `reactivar` | Mantener el código; actualizar `docs/contratos/api-types.ts` y S5.7 | Pendiente |
| 3 | Filtros `?estado=` y `cumpleRequisitos` | Agregarlos | Pendiente |
| 4 | Paginación `PaginaResultado` | Solo si una pantalla la necesita | Diferido |
| 5 | `docs/mensajes-validacion.md` | Agregar "Seleccione un currículum." e "Indique si acepta o declina la oferta." | Pendiente |
| 6 | Fecha de Honduras | Usar `IFechaHonduras` en vez de `DateTime.UtcNow.AddHours(-6)` (`ReglasPostulacion.cs:17`) y `DateTime.UtcNow` (`PostularseHandler.cs:36`) | Pendiente |
| 7 | Cupos de oferta | Evitar que dos ofertas simultáneas superen el cupo (transacción serializable o verificación con bloqueo) | Pendiente |

### 1.4 Estado del tablero vs. realidad

| Columna en el tablero | Issue | Realidad | Mover a |
|---|---|---|---|
| QA | HU-01, HU-E01, HU-A01, HU-05, HU-06, HU-E05, HU-E06, HU-E07 | Solo backend; falta frontend | **In progress** |
| In progress | HU-E03 | Backend fusionado; falta frontend | In progress |
| In progress | HU-02, HU-E02, HU-A03, HU-E04 | Sin código en `main` | In progress (reasignadas, ver 3) |
| In progress | T-06, T-08 | Sin código / esqueleto | In progress (prioridad 1) |
| Ready | T-07 | Sin hacer | **In progress** (prioridad 1) |
| Ready | HU-A04 | — | **Product Backlog** (recorte, ver 5) |
| — | T-09 (#45, nuevo) | Pendientes del PR #44 | **Ready** |

> Tablero actualizado el 20 de noviembre según esta tabla; responsables reasignados según los paquetes de la sección 3. T-06 conserva a CristoferMeza504 como apoyo de DeibiXD.

**Regla del tablero a partir de hoy:** `QA` solo cuando backend **y** frontend están en un PR abierto o fusionado y falta la prueba cruzada; `Finalizado` cuando otra persona la probó **en producción** y el issue se cierra.

---

## 2. Calendario

| Fecha | Meta | Hito |
|---|---|---|
| **sáb 21 – dom 22** | Docker + almacenamiento + pendientes del PR #44 + base del frontend | **H-A (dom 22, noche):** `docker compose up --build` levanta API + BD con datos demo. PR del #44 fusionado. |
| **lun 23 – mar 24** | Despliegue y CI; pantallas principales de cada paquete | **H-B (mar 24, noche):** API en Render, frontend en Vercel, Actions en verde. Login en producción con cada rol. |
| **mié 25 – jue 26** | Resto de pantallas; todo llega a producción por `main` | **H-C (jue 26, noche):** flujo completo de la demo funciona en producción. |
| **vie 27** | Integración, corrección de errores, seed de producción, contratos y documentación al día | **Congelamiento 23:59.** Tag `v1.0-rc`. No se fusionan funcionalidades nuevas después. |
| **sáb 28** | **Pruebas cruzadas** (cada quien prueba el paquete de otro, ver 6), corrección solo de errores | Lista de errores cerrada |
| **dom 29** | **Preparación:** evidencias, ensayo general con el guion, video de respaldo | Tag `v1.0`, ensayo sin fallos |

Reunión corta (20 min) **sáb 21, lun 23, mié 25 y vie 27**. Mensaje diario *ayer / hoy / me bloquea* antes de las 10 a. m. Ningún PR espera más de 12 horas por revisión.

---

## 3. Paquetes de trabajo (5 personas)

Cada paquete incluye backend **y** frontend de lo suyo (Vertical Slicing). Los formularios usan Zod + React Hook Form con `mode: 'onChange'` y **los mismos mensajes** que FluentValidation (`docs/mensajes-validacion.md`).

### P1 — Infraestructura, despliegue y aprobación de empresas · **DeibiXD**

Dueño de las cuentas de Supabase, Render y Vercel (el repo es suyo).

| Fecha | Trabajo | Issue |
|---|---|---|
| sáb 21 – dom 22 | `backend/Dockerfile` multi-stage (`sdk:10.0` → `aspnet:10.0-alpine`), `backend/.dockerignore`, `frontend/Dockerfile` (build de Vite servido con nginx), `docker-compose.yml` con **BD + API + frontend** (la rúbrica IV pide el *sistema completo con un solo comando*), `example.env` actualizado. Prueba: alguien que nunca lo corrió hace `cp example.env .env && docker compose up --build` y entra al frontend y a Swagger. | #28 T-06 |
| lun 23 – mar 24 | BD en la nube (Neon o Supabase, ver sección 4), bucket de Supabase Storage `upractice-archivos`, Render desde el Dockerfile con `/health`, Vercel con `vercel.json`, CORS, `.github/workflows/ci.yml` (backend, frontend, docker) y checks obligatorios en `main`. Todo con HTTPS. | #29 T-07 |
| mié 25 – jue 26 | HU-C01 aprobación de empresas (backend + pantalla `/coordinacion/empresas`). | #2 |
| vie 27 | Seed de demo en producción, variables finales en Render, verificación 7.6 del plan de fases. | — |

### P2 — Almacenamiento, currículums y perfil · **LoadRon**

Además: revisión de PR de backend y mantenimiento del tablero.

| Fecha | Trabajo | Issue |
|---|---|---|
| sáb 21 – dom 22 | T-08: `IAlmacenamientoArchivos` con proveedor local y Supabase Storage (PDF, 5 MB). | #30 |
| lun 23 – mar 24 | HU-03 currículums: subir, listar, descargar, eliminar (backend + `/estudiante/curriculums`). | #8 |
| mié 25 – jue 26 | HU-02 perfil académico y solicitud de verificación (backend + `/estudiante/perfil`); backend de HU-C02 (verificar perfiles). | #7, #19 |
| vie 27 | Actualizar `docs/contratos/api-types.ts` con los endpoints reales; revisión final de backend. | — |

### P3 — Vacantes · **CristoferMeza504**

| Fecha | Trabajo | Issue |
|---|---|---|
| sáb 21 – dom 22 | Frontend de HU-E03: `/empresa/vacantes/nueva` con Zod simétrico a `CrearVacanteValidator`. | #14 |
| lun 23 – mar 24 | HU-E04 panel de vacantes: listar, editar, pausar, cerrar (backend + `/empresa/vacantes`). | #15 |
| mié 25 – jue 26 | HU-04 catálogo para estudiantes: `/vacantes` y `/vacantes/:id`. **Filtrar `Estado == Publicada` a mano** (el filtro global ya no lo hace, ver PR #44). Filtros: texto y carrera. | #9 |
| vie 27 | Solo si todo lo anterior está en producción: HU-E02 reclutadores. | #13 |

### P4 — Postulaciones · **elaleph27**

| Fecha | Trabajo | Issue |
|---|---|---|
| sáb 21 | **PR pequeño con los pendientes del PR #44** (sección 1.3, puntos 1, 2, 3, 5, 6 y 7). Debe fusionarse antes de empezar las pantallas. | #45 T-09 |
| dom 22 – mar 24 | Frontend estudiante: botón *Postular* en el detalle de vacante (HU-05, coordinar con P3), `/estudiante/postulaciones` y detalle con retirar, reactivar y responder oferta (HU-06). | #10, #11 |
| mié 25 – jue 26 | Frontend empresa: `/empresa/vacantes/:id/postulantes` (HU-E05), `/empresa/postulaciones/:id` con CV e índice según privacidad (HU-E06) y cambio de estado (HU-E07). | #16, #17, #18 |
| vie 27 | Prueba de aislamiento en vivo (reclutador de otra empresa → 404) preparada para la demo. | — |

### P5 — Frontend base, registros y administración · **recartedanny**

Líder técnico del frontend: revisa todos los PR de frontend.

| Fecha | Trabajo | Issue |
|---|---|---|
| sáb 21 | Layout con menú por rol, aterrizaje por rol después del login, componentes compartidos (tabla, `ConfirmDialog`, estados de carga y lista vacía), `shared/api/types.ts` sincronizado con el contrato. **Desbloquea a todos: fusionar el sábado.** | #27 (seguimiento) |
| dom 22 – lun 23 | `/registro/estudiante` (HU-01) y `/registro/empresa` + `/empresa/habilitaciones` (HU-E01). | #6, #12 |
| mar 24 – mié 25 | `/admin/universidades` (HU-A01) y HU-A03 cuentas de coordinador (backend + `/admin/coordinadores`). | #20, #22 |
| jue 26 | Frontend de HU-C02: `/coordinacion/estudiantes` (verificar perfiles; backend de P2). | #19 |
| vie 27 | Prueba de simetría Zod ↔ FluentValidation (sección 5.5 del plan de fases) como evidencia. | — |

### Resumen de carga

| Paquete | Persona | Historias / tareas |
|---|---|---|
| P1 | DeibiXD | T-06, T-07, CI, HU-C01 |
| P2 | LoadRon | T-08, HU-03, HU-02, HU-C02 (backend) |
| P3 | CristoferMeza504 | HU-E03 (front), HU-E04, HU-04, HU-E02 (si hay tiempo) |
| P4 | elaleph27 | Pendientes del PR #44, HU-05, HU-06, HU-E05, HU-E06, HU-E07 (front) |
| P5 | recartedanny | Base de frontend, HU-01, HU-E01, HU-A01 (front), HU-A03, HU-C02 (front) |

### Dependencias

- P1 (Docker, sáb–dom) → todos prueban con `docker compose`. Mientras tanto, cada quien usa `dotnet run` con Postgres local.
- P5 (layout, sáb 21) → las pantallas de P2, P3 y P4 usan el menú y los componentes compartidos.
- P4 (pendientes del #44, sáb 21) → las pantallas de postulaciones usan las rutas definitivas.
- P2 (T-08, dom 22) → HU-03 → HU-05 (postularse necesita un CV real). Mientras tanto, P4 usa los CV del seed.
- P3 (detalle de vacante, HU-04) ↔ P4 (botón *Postular*): acordar el componente el lunes 23.

---

## 4. Cumplimiento de los lineamientos (rúbrica)

### 4.1 Rúbrica: qué falta para "Excelente" (81–100 %)

| Criterio | Pide para Excelente | Estado hoy | Acción |
|---|---|---|---|
| I. Análisis y BD | `EmpresaId` claro, **restricciones e índices** en el ERD, backlog ágil | ERD y `EmpresaId` listos; tablero desactualizado | Revisar que el ERD muestre índices únicos y FK (P2, vie 27); tablero al día (LoadRon) |
| II. Backend | Filtros globales, **simetría DTO**, middleware que intercepta FluentValidation | Filtros y middleware listos; DTO de postulaciones desfasados del contrato | Pendientes del #44 (P4) y `api-types.ts` = DTO reales (P2) |
| III. Frontend | Arquitectura vertical, **Zod en tiempo real + extractor de errores**, **UX fluida**, sin *god components* | Base lista; faltan features | Cada pantalla: componentes pequeños, carga, lista vacía, errores por campo (P5 revisa) |
| IV. Docker | `docker-compose.yml` levanta **el sistema completo** con un solo comando | Nada | Compose con BD + API + **frontend** (P1) |
| V. Cloud | **Vercel (front) consumiendo Render (back) conectado a la BD**, todo HTTPS | Nada | P1, hito H-B (mar 24) |

**Decisión de BD en la nube:** los lineamientos aceptan Neon o Supabase, pero el texto de la rúbrica nombra **Neon**. Recomendación: **Neon para PostgreSQL** (coincide literal con la rúbrica) y **Supabase solo para Storage** (archivos de CV). Si el equipo prefiere una sola cuenta, Supabase para ambos también cumple los lineamientos. Decidir en la reunión del sábado 21.

### 4.2 Lineamientos por fase

| Fase (peso) | Requisito | Estado hoy | Responsable |
|---|---|---|---|
| **1 (15 %)** | Pitch de 1 página | Listo (`docs/Documentos - UPractice/pitch.pdf`) | — |
| | ERD 3FN con `EmpresaId` | Listo (`docs/diagrama 4.png`) | — |
| | Kanban con HU claras | Desactualizado → corregir hoy (sección 1.4) | LoadRon |
| | Contratos de interfaz | Desfasados en postulaciones → P4 (sáb) y P2 (vie) | elaleph27, LoadRon |
| **2 (25 %)** | Clean Architecture | Listo | — |
| | Code-First + `HasQueryFilter` | Listo | — |
| | JWT con `EmpresaId` cifrado | Listo | — |
| | FluentValidation + middleware | Listo; cada endpoint nuevo con su validador | Todos |
| **3 (25 %)** | Vertical Slicing (`features/*`) | Estructura lista; faltan casi todas las features | P2–P5 |
| | Rutas protegidas | Listo | — |
| | Zod + RHF `onChange` simétrico | Solo en auth | Todos |
| | Extractor universal + interceptores | Listo | — |
| **4 (15 %)** | Dockerfile multi-stage SDK → Alpine | **Falta** | DeibiXD |
| | Compose BD efímera + API + frontend (un solo comando) | **Falta** | DeibiXD |
| **5 (20 %)** | BD en Neon o Supabase | **Falta** | DeibiXD |
| | API en Render desde Dockerfile (HTTPS) | **Falta** | DeibiXD |
| | Frontend en Vercel desde `main` | **Falta** | DeibiXD |
| | Push a `main` actualiza todo (CI) | **Falta** | DeibiXD |

---

## 5. Recorte si hay atraso

Se decide en las reuniones del **lun 23** y **mié 25**. Nunca se recorta un requisito de los lineamientos.

1. **HU-A04** suspensión de empresas: ya queda fuera (Product Backlog).
2. **HU-E02** reclutadores: la empresa trabaja con su `AdminEmpresa` y los reclutadores del seed.
3. **HU-A02** catálogo académico: solo por seed, sin pantalla.
4. **HU-03** reducida a un solo CV por estudiante.
5. **HU-04** solo búsqueda por texto y carrera.
6. **HU-E06** sin descarga de CV (solo datos del perfil).

Si el **martes 24** Render o Vercel no funcionan, P5 apoya a P1 el miércoles: sin despliegue se pierde el 20 %.

---

## 6. Pruebas y preparación (sáb 28 – dom 29)

### 6.1 Pruebas cruzadas (sábado 28, en producción)

| Prueba | Paquete probado |
|---|---|
| DeibiXD | P4 postulaciones (estudiante y empresa, aislamiento 404) |
| LoadRon | P3 vacantes |
| CristoferMeza504 | P5 registros y administración |
| elaleph27 | P2 currículums, perfil y verificación |
| recartedanny | P1 Docker desde cero, despliegue automático, aprobación de empresas |

Cada error se registra como issue con la plantilla `bug` y se corrige el mismo día. Después de las 20:00 del sábado solo se fusionan correcciones revisadas.

### 6.2 Preparación (domingo 29)

- [ ] Seed de demo en producción y cuentas verificadas (ver `credenciales-locales.md`, **no se sube**).
- [ ] Evidencias: capturas de Actions en verde, Render y Vercel, `docker images` con la imagen Alpine, token en jwt.io ilegible, prueba de aislamiento en verde, prueba de simetría Zod ↔ FluentValidation.
- [ ] Tablero: todas las HU del alcance en `Finalizado`; el resto en `Product Backlog` con su sprint.
- [ ] Ensayo completo con el guion de la sección 9.2 del plan de fases.
- [ ] Video de respaldo del flujo completo y laptop con `docker compose up` lista.
- [ ] Tag `v1.0`.
