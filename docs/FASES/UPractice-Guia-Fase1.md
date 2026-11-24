# UPractice — Guía paso a paso para completar la Fase 1

> Qué hacer, en qué orden y quién lo hace para dejar la Fase 1 terminada y el equipo listo para programar.
> Documento de referencia: `UPractice-Fase1.md` (el entregable).
> Fecha objetivo para cerrar la Fase 1: **domingo 8 de noviembre de 2026**.

**Supuestos.** Cada integrante tiene cuenta de GitHub, Git configurado, JetBrains Rider (o el IDE de JetBrains que usen para .NET), Visual Studio Code, Docker Desktop y Node.js. El tablero ya existe en `https://github.com/users/DeibiXD/projects/3`, así que el dueño de la cuenta **DeibiXD** hace los pasos de configuración y el resto del equipo trabaja en los pasos marcados como *Todo el equipo*.

**Contenido**

1. [Resumen de pasos](#1-resumen-de-pasos)
2. [Repartir los roles del equipo](#2-repartir-los-roles-del-equipo)
3. [Crear y configurar el repositorio](#3-crear-y-configurar-el-repositorio)
4. [Subir la documentación con el primer Pull Request](#4-subir-la-documentación-con-el-primer-pull-request)
5. [Configurar el tablero (GitHub Projects)](#5-configurar-el-tablero-github-projects)
6. [Crear los issues](#6-crear-los-issues)
7. [Exportar el ERD y el pitch](#7-exportar-el-erd-y-el-pitch)
8. [Revisión final y entrega](#8-revisión-final-y-entrega)
9. [Anexos: contenido de archivos](#9-anexos-contenido-de-archivos)

---

## 1. Resumen de pasos

| # | Paso | Responsable | Tiempo aprox. |
|---|---|---|---|
| 1 | Repartir roles y responsables por funcionalidad | Todo el equipo | 20 min |
| 2 | Crear el repositorio y agregar colaboradores | DeibiXD | 15 min |
| 3 | Configurar reglas de `main`, merges y etiquetas | DeibiXD | 15 min |
| 4 | Subir documentos y plantillas en el primer PR | Responsable de documentación | 30 min |
| 5 | Configurar campos, vistas y automatizaciones del tablero | DeibiXD | 30 min |
| 6 | Crear los issues (HU y tareas técnicas) y llenar sus campos | Todo el equipo (repartido) | 60 min |
| 7 | Exportar el ERD como imagen y el pitch como PDF | Responsable de documentación | 20 min |
| 8 | Revisión cruzada contra la lista final y entrega | Todo el equipo | 30 min |

---

## 2. Repartir los roles del equipo

*Todo el equipo* — una reunión corta para decidir quién es quién. La propuesta está pensada para **4 personas**; si son 3, la Persona D también asume el trabajo de C; si son 5, la quinta persona se suma al frontend de las pantallas de empresa.

| Persona | Rol permanente | Sprint 0 (base técnica) | Sprint 1 (funcionalidad de punta a punta) |
|---|---|---|---|
| **A** | Líder técnico backend · revisa PR de backend | T-01, T-02, T-03 | Cuentas y administración: HU-01, HU-E01, HU-A01, HU-A02, HU-A03 |
| **B** | Responsable de documentación y tablero | T-04, T-08 | Perfil y coordinación: HU-02, HU-03, HU-C01, HU-C02 |
| **C** | Líder técnico frontend · revisa PR de frontend | T-05 | Vacantes: HU-E03, HU-E04, HU-04, HU-E02, HU-A04 |
| **D** | Responsable DevOps (Docker y nube) | T-06, T-07 | Postulaciones: HU-05, HU-06, HU-E05, HU-E06, HU-E07 |

Además, definan:

- **Product Owner** (puede ser A o B): ordena el backlog y decide dudas de alcance.
- **Canal del equipo** (WhatsApp o Discord) con un mensaje diario corto: *ayer hice / hoy haré / me bloquea*.
- **Dos reuniones fijas por semana** (por ejemplo, lunes y jueves, 20 minutos) para revisar el tablero.

---

## 3. Crear y configurar el repositorio

### 3.1 Crear el repositorio

*Responsable: DeibiXD*

1. En GitHub: **New repository**.
2. Owner: `DeibiXD`. Nombre: `upractice`. Descripción: *Plataforma SaaS B2B para gestionar prácticas profesionales entre empresas, estudiantes y universidades.*
3. Visibilidad: **Public** (recomendado).
   > Las reglas de protección de rama en repositorios **privados** requieren un plan de pago (GitHub Pro o Team). Si prefieren privado, el dueño puede activar GitHub Pro gratis con el GitHub Student Developer Pack; sin eso, las reglas del paso 3.3 no se aplican. En un repositorio público no hay riesgo porque los secretos nunca se suben (van en variables de entorno).
4. Marcar **Add a README file**.
5. **.gitignore template:** `VisualStudio`. Las líneas de Node se agregan en el paso 4.
6. **Create repository**.

### 3.2 Agregar a los compañeros

1. En el repositorio: **Settings → Collaborators → Add people**.
2. Agregar a cada integrante por usuario de GitHub. Cada uno debe **aceptar la invitación** (llega por correo y a las notificaciones de GitHub).

### 3.3 Proteger la rama `main`

1. **Settings → Rules → Rulesets → New ruleset → New branch ruleset**.
2. Nombre: `proteger-main`. **Enforcement status:** Active.
3. **Target branches → Add target → Include default branch**.
4. Activar estas reglas:
   - **Restrict deletions**.
   - **Block force pushes**.
   - **Require a pull request before merging** → *Required approvals*: **1**. Activar también *Dismiss stale pull request approvals when new commits are pushed*.
   - **Require status checks to pass** → **dejarlo apagado por ahora.** Se activa en la Fase 5, cuando exista el flujo de CI.
5. **Create**.

### 3.4 Configurar cómo se fusionan los PR

1. **Settings → General → Pull Requests**.
2. Dejar solo **Allow squash merging** (desmarcar *merge commits* y *rebase merging*). Así cada historia queda como un solo commit limpio en `main`.
3. Marcar **Automatically delete head branches**.

### 3.5 Crear las etiquetas

1. **Issues → Labels**.
2. Borrar las que no se usarán (`duplicate`, `good first issue`, `help wanted`, `invalid`, `question`, `wontfix`). Conservar `bug`.
3. Crear estas etiquetas:

| Etiqueta | Color sugerido | Uso |
|---|---|---|
| `historia` | `#1D76DB` | Historia de usuario (HU-xx) |
| `tarea` | `#5319E7` | Historia técnica (T-xx) o sub-tarea |
| `bug` | `#D73A4A` | Error |
| `backend` | `#0E8A16` | Toca la API .NET |
| `frontend` | `#FBCA04` | Toca la app React |
| `devops` | `#B60205` | Docker, CI, despliegue |
| `docs` | `#C5DEF5` | Documentación |

---

## 4. Subir la documentación con el primer Pull Request

*Responsable: Persona B.* Este primer PR sirve también para que todo el equipo practique el flujo que se usará el resto del proyecto.

### 4.1 Clonar y crear la rama

```bash
git clone https://github.com/DeibiXD/upractice.git
cd upractice
git switch -c docs/fase-1
```

En Visual Studio Code: **File → Open Folder → upractice**.

### 4.2 Crear la estructura y copiar los archivos

```
upractice/
├─ .github/
│  ├─ ISSUE_TEMPLATE/
│  │  ├─ historia-usuario.md      ← Anexo A
│  │  ├─ tarea-tecnica.md         ← Anexo B
│  │  ├─ bug.md                   ← Anexo C
│  │  └─ config.yml               ← Anexo D
│  └─ pull_request_template.md    ← Anexo E
├─ docs/
│  ├─ fase-1/
│  │  ├─ UPractice-Fase1.md       ← el entregable
│  │  ├─ pitch.md                 ← copia de la sección 1 del entregable
│  │  ├─ erd.mmd                  ← paso 7
│  │  ├─ erd.svg / erd.png        ← paso 7
│  │  └─ pitch.pdf                ← paso 7
│  ├─ UPractice-Guia-Fase1.md     ← esta guía
│  └─ UPractice-Plan-Fases.md     ← plan de las fases 2 a 5
├─ .gitignore                     ← agregar líneas del Anexo F
└─ README.md                      ← reemplazar por el Anexo G
```

`backend/` y `frontend/` **no** se crean ahora: se crean en el Sprint 0 (tareas T-01 y T-05).

### 4.3 Commit, push y Pull Request

```bash
git add .
git commit -m "docs(fase-1): entregable, plantillas de issues y PR"
git push -u origin docs/fase-1
```

1. En GitHub aparecerá el botón **Compare & pull request**.
2. Título: `docs: entregable de la Fase 1`. La descripción se llena con la plantilla del Anexo E.
3. **Reviewers:** asignar al menos a una persona.
4. El revisor revisa en la pestaña **Files changed**. Si todo está bien, **Review changes → Approve**.
5. **Squash and merge**. La rama se borra sola.
6. Todos actualizan su copia local:

```bash
git switch main
git pull
```

### 4.4 Verificar en GitHub

- Abrir `docs/fase-1/UPractice-Fase1.md` en GitHub y confirmar que **el ERD y la máquina de estados se ven como diagramas** (GitHub dibuja Mermaid automáticamente).
- En **Issues → New issue** deben aparecer las plantillas *Historia de usuario*, *Tarea técnica* y *Bug*.

---

## 5. Configurar el tablero (GitHub Projects)

*Responsable: DeibiXD.* Todo se hace dentro de `https://github.com/users/DeibiXD/projects/3`.

### 5.1 Datos generales y acceso

1. Menú **⋯ (arriba a la derecha) → Settings**.
2. **Project name:** `UPractice — Kanban`. **Short description:** *Tablero del proyecto final: SaaS de prácticas profesionales.*
3. **README** del proyecto: pegar los enlaces a `docs/fase-1/UPractice-Fase1.md`, la Definition of Ready y la Definition of Done (S4.2 y S4.3 del entregable).
4. **Manage access → Invite collaborators:** agregar a cada integrante con rol **Write**. Si el proyecto es **Private**, el catedrático no podrá verlo: cambiar **Visibility** a *Public* o invitarlo como lector.

### 5.2 Vincular el repositorio

En el repositorio `upractice`: pestaña **Projects → Link a project → UPractice — Kanban**.

### 5.3 Campos

En **Settings → Custom fields** (o desde el encabezado de cualquier columna en la vista de tabla):

| Campo | Tipo | Opciones |
|---|---|---|
| **Status** *(ya existe; editar opciones)* | Single select | `Backlog` · `Ready` · `In Progress` · `In Review` · `Done` |
| **Sprint** | Single select | `Sprint 0` · `Sprint 1` · `Cierre` · `Sprint 2` · `Futuro` |
| **Prioridad** | Single select | `Must` · `Should` · `Could` · `Won't` |
| **Estimación** | Number | (1, 2, 3, 5 u 8 puntos) |
| **Área** | Single select | `Backend` · `Frontend` · `Full-stack` · `DevOps` · `Docs` |
| **Fase** | Single select | `Fase 1` · `Fase 2` · `Fase 3` · `Fase 4` · `Fase 5` |

> Si el tablero ya tenía tarjetas en `Todo`, cámbienlas a `Backlog` **antes** de borrar la opción `Todo`.

### 5.4 Vistas

| Vista | Tipo | Configuración |
|---|---|---|
| **Kanban** | Board | *Column by:* `Status`. *Filter:* `-status:Done sprint:"Sprint 0","Sprint 1","Cierre"` (opcional). |
| **Por sprint** | Table | *Group by:* `Sprint`. Columnas visibles: Title, Assignees, Status, Prioridad, Estimación, Área. *Field sum:* Estimación. |
| **Por responsable** | Table | *Group by:* `Assignees`. *Sort by:* Status. |
| **Backlog** | Table | *Filter:* `status:Backlog`. *Sort by:* Prioridad. |

Guardar cada vista con **Save changes**.

### 5.5 Automatizaciones

Menú **⋯ → Workflows**. Activar:

| Workflow | Configuración |
|---|---|
| **Item added to project** | Poner Status = `Backlog` |
| **Auto-add to project** | Repositorio `DeibiXD/upractice`, filtro `is:issue` |
| **Item closed** | Poner Status = `Done` |
| **Pull request merged** | Poner Status = `Done` |
| **Item reopened** | Poner Status = `In Progress` |

Con **Auto-add**, todo issue nuevo del repositorio entra solo al tablero.

### 5.6 Cómo se mueve una tarjeta (acuerdo del equipo)

| Columna | Quién la mueve | Cuándo |
|---|---|---|
| `Backlog → Ready` | Product Owner | La HU cumple la Definition of Ready (S4.2 del entregable). |
| `Ready → In Progress` | El responsable | Al crear su rama (`feat/HU-05-postulacion`). Se asigna a sí mismo. |
| `In Progress → In Review` | El responsable | Al abrir el PR con `Closes #<número>` en la descripción. |
| `In Review → Done` | Automático | Al fusionar el PR (el issue se cierra solo gracias a `Closes #`). |

---

## 6. Crear los issues

*Todo el equipo.* Cada persona crea **los issues que le tocan** según la tabla del paso 2, y Persona B crea los del backlog posterior.

### 6.1 Cómo crear cada issue

1. **Issues → New issue → Historia de usuario** (o *Tarea técnica*).
2. **Título:** `HU-05 · Postulación a vacantes` (código + nombre, igual que en el entregable).
3. **Cuerpo:** copiar desde `UPractice-Fase1.md` la historia completa (*Como / Quiero / Para* y criterios de aceptación) y llenar *Contrato* con los endpoints de la tabla S5.5 que tienen ese código de HU.
4. **Labels:** `historia` + área (`backend`, `frontend`, o ambas).
5. **Assignees:** el responsable del paso 2.
6. **Projects:** `UPractice — Kanban` (si *Auto-add* está activo, entra solo).
7. **Create**. Luego, en el panel derecho del issue (sección *Projects*), llenar **Sprint, Prioridad, Estimación, Área y Fase**.

### 6.2 Lista de issues

**Historias técnicas** (label `tarea`; Fase 2 salvo que se indique)

| Título | Sprint | Prioridad | Estim. | Área | Fase | Responsable |
|---|---|---|---|---|---|---|
| T-01 · Monorepo y solución .NET con Clean Architecture | Sprint 0 | Must | 2 | Backend | Fase 2 | A |
| T-02 · DbContext, configuraciones, migración inicial y seed | Sprint 0 | Must | 5 | Backend | Fase 2 | A |
| T-03 · JWT cifrado, ICurrentUser y filtros globales por tenant | Sprint 0 | Must | 5 | Backend | Fase 2 | A |
| T-04 · Middleware de errores y validación con FluentValidation | Sprint 0 | Must | 3 | Backend | Fase 2 | B |
| T-05 · Frontend base: rutas protegidas, Axios y extractor de errores | Sprint 0 | Must | 5 | Frontend | Fase 3 | C |
| T-06 · Dockerfile multi-stage y docker-compose | Sprint 0 | Must | 3 | DevOps | Fase 4 | D |
| T-07 · Despliegue esqueleto en Supabase, Render y Vercel | Sprint 0 | Must | 3 | DevOps | Fase 5 | D |
| T-08 · Almacenamiento de archivos (local y Supabase Storage) | Sprint 0 | Must | 3 | Backend | Fase 2 | B |

**Historias de usuario del Sprint 1** (label `historia`; Área `Full-stack`; Fase `Fase 2`)

> Cada HU es de punta a punta (endpoint + pantalla). Se clasifican en la Fase 2 porque su núcleo es el backend; la pantalla queda dentro de la misma HU.

| Título | Prioridad | Estim. | Responsable |
|---|---|---|---|
| HU-01 · Registro e inicio de sesión con correo institucional | Must | 5 | A |
| HU-02 · Completar perfil académico y solicitar verificación | Must | 5 | B |
| HU-03 · Gestión de versiones de currículum (CV) | Must | 3 | B |
| HU-04 · Exploración y filtrado de vacantes | Must | 5 | C |
| HU-05 · Postulación a vacantes | Must | 5 | D |
| HU-06 · Panel de seguimiento de postulaciones | Must | 3 | D |
| HU-E01 · Registro de empresa y solicitud de habilitación | Must | 5 | A |
| HU-E02 · Gestión de reclutadores de la empresa | Must | 3 | C |
| HU-E03 · Creación y publicación de vacantes | Must | 5 | C |
| HU-E04 · Panel de control de vacantes | Must | 3 | C |
| HU-E05 · Listado de postulantes | Must | 3 | D |
| HU-E06 · Revisión de perfil y CV verificados | Must | 3 | D |
| HU-E07 · Gestión del estado de la postulación | Must | 3 | D |
| HU-C01 · Aprobación de empresas | Must | 3 | B |
| HU-C02 · Verificación de perfiles académicos | Must | 3 | B |
| HU-A01 · Gestión de universidades | Must | 3 | A |
| HU-A02 · Gestión del catálogo académico y de habilidades | Must | 3 | A |
| HU-A03 · Gestión de cuentas de coordinador | Must | 3 | A |
| HU-A04 · Supervisión y suspensión de empresas | Should | 3 | C |

Todas con **Sprint = `Sprint 1`**.

**Backlog posterior** (label `historia`; Sprint `Sprint 2`; Prioridad `Could`; sin responsable ni estimación por ahora)

HU-07, HU-08, HU-09, HU-10, HU-E08, HU-E09, HU-C03, HU-C04 — con el texto de la sección S4.7 del entregable.

**Visión futura** (opcional): un solo issue `Visión futura` con la lista de S4.7, Sprint `Futuro`, Prioridad `Won't`.

### 6.3 Sub-issues

Si una HU la trabajan dos personas (por ejemplo, alguien hace solo la pantalla), abrir la HU y usar **Create sub-issue** para separar:

- `[HU-05] Backend: endpoint, reglas y validador`
- `[HU-05] Frontend: pantalla y esquema Zod`

Si la HU la hace una sola persona, basta con la lista de verificación (*Definition of Done*) dentro del issue.

### 6.4 Atajo opcional con GitHub CLI

Si alguien del equipo tiene GitHub CLI (`gh`) con sesión iniciada, puede crear las etiquetas y los issues desde la terminal. Ejemplo (Git Bash o terminal de VS Code):

```bash
# Etiquetas
gh label create historia --color 1D76DB --repo DeibiXD/upractice
gh label create tarea    --color 5319E7 --repo DeibiXD/upractice
gh label create backend  --color 0E8A16 --repo DeibiXD/upractice
gh label create frontend --color FBCA04 --repo DeibiXD/upractice
gh label create devops   --color B60205 --repo DeibiXD/upractice
gh label create docs     --color C5DEF5 --repo DeibiXD/upractice

# Un issue (el cuerpo se guarda antes en un archivo .md)
gh issue create --repo DeibiXD/upractice \
  --title "HU-05 · Postulación a vacantes" \
  --body-file issues/HU-05.md \
  --label historia,backend,frontend \
  --project "UPractice — Kanban"
```

Los campos personalizados (Sprint, Prioridad, etc.) igual se llenan después desde la vista de tabla del tablero: ahí se pueden editar varias filas rápido, como en una hoja de cálculo.

---

## 7. Exportar el ERD y el pitch

*Responsable: Persona B.*

### 7.1 ERD como imagen

1. Copiar **solo el contenido** del diagrama Mermaid de la sección S3.4 del entregable (desde la línea `erDiagram` hasta la línea anterior al cierre del bloque de código) a `docs/fase-1/erd.mmd`.
2. Generar la imagen con Node (la primera vez descarga el generador; tarda un poco):

```bash
npx -y -p @mermaid-js/mermaid-cli mmdc -i docs/fase-1/erd.mmd -o docs/fase-1/erd.svg
npx -y -p @mermaid-js/mermaid-cli mmdc -i docs/fase-1/erd.mmd -o docs/fase-1/erd.png -s 2 -b white
```

   Alternativa sin terminal: pegar el contenido en [mermaid.live](https://mermaid.live) y usar **Actions → PNG / SVG**.
3. Abrir la imagen y comprobar que se lean todas las tablas.

### 7.2 Pitch en PDF

1. Copiar la sección **1. Pitch** del entregable a `docs/fase-1/pitch.md`.
2. Abrir `pitch.md` en GitHub (después de subirlo), imprimir con **Ctrl + P** y elegir **Guardar como PDF**.
3. Verificar que quepa en **una página**. Si no cabe, reducir márgenes en la ventana de impresión o recortar la tabla de actores.
4. Guardar como `docs/fase-1/pitch.pdf`.

### 7.3 Subir los archivos

Igual que en el paso 4: rama `docs/fase-1-exportes`, commit, push, PR, aprobación y *Squash and merge*.

---

## 8. Revisión final y entrega

*Todo el equipo.* Cada integrante revisa **una** parte que no hizo y marca la casilla.

### 8.1 Lista final según los lineamientos

**Definición del SaaS**
- [ ] `pitch.pdf` existe y ocupa una página.
- [ ] Explica problema, solución, actores, modelo de negocio SaaS B2B y alcance del MVP.

**Diseño multi-empresa (ERD en 3FN)**
- [ ] El ERD se ve como diagrama en GitHub y existe como imagen (`erd.png` / `erd.svg`).
- [ ] Todas las tablas transaccionales principales (`Vacantes`, `Postulaciones`, `HabilitacionesEmpresa`, `Practicas`, `DocumentosPractica`, `Evaluaciones*`) tienen `EmpresaId` como FK.
- [ ] La justificación de 3FN (S3.7) está incluida.

**Tablero Kanban**
- [ ] Todas las HU y tareas técnicas son **issues reales** (no borradores) y están en el tablero.
- [ ] Cada issue del Sprint 0 y Sprint 1 tiene responsable, sprint, prioridad y estimación.
- [ ] Cada HU tiene formato *Como / Quiero / Para* y criterios de aceptación.
- [ ] Las vistas y automatizaciones del paso 5 están activas.
- [ ] El tablero es visible para el catedrático.

**Contratos de interfaz**
- [ ] Endpoints, DTO, formato de errores y reglas de validación (S5 del entregable) están en el repositorio.

**Repositorio**
- [ ] `main` protegida con PR obligatorio y 1 aprobación.
- [ ] Plantillas de issue y PR funcionando.
- [ ] Todos los integrantes hicieron al menos una revisión o un commit (queda como evidencia de trabajo en equipo).

### 8.2 Qué entregar

Según lo que pida el catedrático, normalmente basta con:

1. Enlace al repositorio: `https://github.com/DeibiXD/upractice`.
2. Enlace al tablero: `https://github.com/users/DeibiXD/projects/3`.
3. Enlace directo al entregable: `docs/fase-1/UPractice-Fase1.md` (y `pitch.pdf` / `erd.png` si piden archivos).

### 8.3 Al terminar

- Persona D crea un **tag** de la entrega para poder volver a este punto:

```bash
git switch main && git pull
git tag -a fase-1 -m "Entrega Fase 1"
git push origin fase-1
```

- Mover a `Ready` todas las tareas `T-01` a `T-08`: el lunes 9 de noviembre empieza el Sprint 0 (ver `UPractice-Plan-Fases.md`).

---

## 9. Anexos: contenido de archivos

### Anexo A — `.github/ISSUE_TEMPLATE/historia-usuario.md`

```markdown
---
name: Historia de usuario
about: Funcionalidad vista desde un usuario (HU-xx)
title: "HU-XX · "
labels: historia
---

**Como** <rol> **quiero** <acción> **para** <beneficio>.

### Criterios de aceptación
- [ ] Dado … cuando … entonces …
- [ ] …

### Contrato
- Endpoint(s): `MÉTODO /api/v1/...`
- DTO: `...Request` / `...Dto`

### Notas técnicas
- Tabla(s):
- Regla de tenant:

### Definition of Done
- [ ] Cumple todos los criterios de aceptación
- [ ] Validador FluentValidation + esquema Zod simétrico
- [ ] Prueba de aislamiento entre empresas (si toca datos de empresa)
- [ ] Swagger actualizado
- [ ] PR aprobado, fusionado y desplegado
```

### Anexo B — `.github/ISSUE_TEMPLATE/tarea-tecnica.md`

```markdown
---
name: Tarea técnica
about: Trabajo técnico sin usuario final directo (T-xx)
title: "T-XX · "
labels: tarea
---

### Objetivo


### Pasos / alcance
- [ ] …

### Criterio de terminado
- [ ] …
- [ ] PR aprobado y fusionado
```

### Anexo C — `.github/ISSUE_TEMPLATE/bug.md`

```markdown
---
name: Bug
about: Algo no funciona como dice la historia de usuario
title: "Bug: "
labels: bug
---

### Qué pasa


### Qué debería pasar


### Pasos para reproducir
1.
2.

### Ambiente
- [ ] Local (Docker)
- [ ] Producción (Vercel / Render)

### Evidencia
(captura, respuesta de la API, consola del navegador)
```

### Anexo D — `.github/ISSUE_TEMPLATE/config.yml`

```yaml
blank_issues_enabled: false
```

### Anexo E — `.github/pull_request_template.md`

```markdown
## Qué cambia


## Issue
Closes #

## Cómo probarlo
1.

## Lista de verificación
- [ ] La rama está actualizada con `main`
- [ ] Compila y las pruebas pasan
- [ ] Si cambió un endpoint o DTO, se actualizó `docs/fase-1/UPractice-Fase1.md` S5
- [ ] Si agregó una migración, se generó sobre la última versión de `main`
- [ ] No incluye secretos (claves, contraseñas, cadenas de conexión)
```

### Anexo F — líneas para agregar al `.gitignore`

```gitignore
# Node / Vite
node_modules/
dist/
*.local

# Variables de entorno (nunca subir secretos)
.env
.env.*
!example.env

# JetBrains
.idea/

# Archivos subidos en desarrollo
backend/storage/
```

### Anexo G — `README.md` de la raíz

````markdown
# UPractice

Plataforma SaaS B2B que conecta empresas, estudiantes próximos a graduarse y coordinaciones académicas para gestionar la práctica profesional de punta a punta.

Proyecto final · Plataforma Web SaaS Multi-empresa.

## Documentación


## Stack
- Backend: .NET + Entity Framework Core + PostgreSQL (Clean Architecture)
- Frontend: React + TypeScript + Vite
- Contenedores: Docker / Docker Compose
- Nube: Supabase (BD y archivos) · Render (API) · Vercel (frontend)

## Equipo
| Integrante | Rol |
|---|---|
| | Líder técnico backend |
| | Documentación y tablero |
| | Líder técnico frontend |
| | DevOps |

## Cómo contribuir
1. Tomar un issue en `Ready` y asignárselo.
2. Crear rama `feat/HU-xx-descripcion` desde `main`.
3. Abrir PR con `Closes #<issue>` y pedir revisión.
4. Fusionar con *Squash and merge* después de 1 aprobación.
````
