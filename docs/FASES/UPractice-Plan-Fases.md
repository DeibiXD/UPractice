# UPractice — Plan de las Fases 2 a 5

> Cómo llevar el proyecto desde el diseño (Fase 1) hasta la presentación.
> Hoy: **sábado 7 de noviembre de 2026** · Presentación: **primera semana de diciembre (lunes 30 de noviembre a viernes 4 de diciembre)**.
> Documentos relacionados: `docs/fase-1/UPractice-Fase1.md` (diseño y contratos) y `docs/UPractice-Guia-Fase1.md` (configuración de GitHub).

**Contenido**

1. [Estrategia general](#1-estrategia-general)
2. [Calendario](#2-calendario)
3. [Reparto y forma de trabajo](#3-reparto-y-forma-de-trabajo)
4. [Fase 2 — Backend transaccional (25 %)](#4-fase-2--backend-transaccional-25-)
5. [Fase 3 — Frontend web reactivo (25 %)](#5-fase-3--frontend-web-reactivo-25-)
6. [Fase 4 — Contenerización (15 %)](#6-fase-4--contenerización-15-)
7. [Fase 5 — Despliegue cloud automatizado (20 %)](#7-fase-5--despliegue-cloud-automatizado-20-)
8. [Riesgos y plan de recorte](#8-riesgos-y-plan-de-recorte)
9. [Preparación de la presentación](#9-preparación-de-la-presentación)
10. [Lista final por lineamiento](#10-lista-final-por-lineamiento)

---

## 1. Estrategia general

Quedan unas **3 semanas de trabajo**. Para que alcance, el plan sigue cuatro reglas:

1. **Las fases 2 a 5 avanzan en paralelo, no una después de otra.** Docker (Fase 4) y el despliegue (Fase 5) se montan en la primera semana con una API casi vacía. Así, los problemas de configuración aparecen temprano y cada funcionalidad nueva llega sola a producción.
2. **Cada persona construye funcionalidades completas** (endpoint + pantalla), no "solo backend" o "solo frontend". Esto cumple el *Vertical Slicing* que piden los lineamientos y evita que alguien se quede esperando a otro.
3. **El alcance de la presentación es el Sprint 1 (MVP).** El Sprint 2 (práctica en curso, expediente, evaluaciones, panel de indicadores) solo se toca si el Sprint 1 termina antes de tiempo.
4. **Lo que evalúan los lineamientos va primero.** Filtros globales por `EmpresaId`, JWT cifrado, FluentValidation con middleware, Zod simétrico, interceptores de Axios, Dockerfile multi-stage, Compose y despliegue automático valen el 85 % de la nota. Una pantalla extra no compensa que falte uno de ellos.

---

## 2. Calendario

| Fechas | Etapa | Meta | Hito de salida |
|---|---|---|---|
| sáb 7 – dom 8 nov | **Cierre de Fase 1** | Repositorio, tablero e issues listos (guía de la Fase 1). | Tag `fase-1` y tareas T-01 a T-08 en `Ready`. |
| lun 9 – mié 11 nov | **Sprint 0 — Base técnica** | Esqueleto completo: solución .NET, BD, JWT cifrado, filtros, middleware, frontend base, Docker y despliegue. | **H0 (mié 11):** `docker compose up` funciona y `/health` responde en Render; el frontend en Vercel inicia sesión con el SuperAdmin del seed. |
| jue 12 – mié 18 nov | **Sprint 1 — semana 1** | Núcleo del flujo: cuentas, perfil, habilitación, vacantes. | **H1 (mié 18):** en producción, una empresa registrada y aprobada publica una vacante, y un estudiante verificado la ve. |
| jue 19 – mié 25 nov | **Sprint 1 — semana 2** | Postulación y embudo completo, administración, CV. | **H2 (mié 25):** las 19 HU del Sprint 1 en producción. **Congelamiento de funcionalidades.** |
| jue 26 – dom 29 nov | **Cierre** | Pruebas cruzadas, corrección de errores, datos de demostración, evidencias, ensayos. | **H3 (dom 29):** tag `v1.0`, ensayo general completo sin fallos. |
| lun 30 nov – vie 4 dic | **Presentación** | Presentar. No se despliega nada nuevo esta semana salvo una corrección crítica. | — |

### 2.1 Detalle del Sprint 0

| Día | Persona A | Persona B | Persona C | Persona D |
|---|---|---|---|---|
| **lun 9** | **T-01** solución .NET (primero, a media mañana: desbloquea al resto) → empieza T-02 | Lee S5.3 y S5.7 del diseño; prepara T-04 | **T-05** proyecto Vite, router, estructura `features/` | Crea proyectos en Supabase, Render y Vercel (S7.1–7.3); borrador del Dockerfile |
| **mar 10** | **T-02** DbContext, configuraciones, migración, seed | **T-04** middleware y filtro de validación | **T-05** Axios, interceptores, extractor de errores, guardias | **T-06** Dockerfile y Compose |
| **mié 11** | **T-03** JWT cifrado, `ICurrentUser`, filtros, prueba de aislamiento | **T-08** almacenamiento local y Supabase | Pantalla de login conectada a la API + cambio de contraseña | **T-07** despliegue esqueleto + CI (GitHub Actions) |

### 2.2 Orden dentro del Sprint 1

Gracias al **seed de demostración** (universidades, carreras, empresas aprobadas, estudiantes verificados), nadie necesita esperar a que otro termine su pantalla para probar la suya.

| Persona | Semana 1 (12–18 nov) | Semana 2 (19–25 nov) |
|---|---|---|
| **A** | HU-01, HU-E01 (registro y login; prioridad del jueves 12) | HU-A01, HU-A02, HU-A03 |
| **B** | HU-02, HU-C02 (perfil y verificación) | HU-C01, HU-03 |
| **C** | HU-E03, HU-E04 (vacantes de la empresa) | HU-04, HU-E02, HU-A04 |
| **D** | HU-05 (postulación) | HU-06, HU-E05, HU-E06, HU-E07 |

### 2.3 Si sobra tiempo después de H2

En este orden, **solo después del congelamiento y si todo está estable**: panel de indicadores básico (HU-C04, luce mucho en la demo) → registro de inicio y cierre de práctica (HU-07) → evaluación de la empresa (HU-09) y reputación visible (HU-10).

---

## 3. Reparto y forma de trabajo

### 3.1 Roles permanentes

| Persona | Rol | Revisa los PR de |
|---|---|---|
| A | Líder técnico backend | Backend |
| B | Documentación, tablero y Product Owner | Documentación y lógica de negocio |
| C | Líder técnico frontend | Frontend |
| D | DevOps (Docker, CI, nube) | Dockerfile, Compose, workflows, configuración |

### 3.2 Rutina

- **Mensaje diario** en el canal del equipo antes de las 10 a. m.: *ayer / hoy / me bloquea*.
- **Reunión corta** lunes y jueves (20 min): revisar el tablero por responsable y destrabar.
- **Revisión de hito** al cierre de cada hito (mié 11, mié 18, mié 25): demo interna en producción, no en la máquina de nadie.
- **Regla de PR:** ningún PR espera más de 12 horas por revisión. PR pequeños (una HU o una parte de ella).

### 3.3 Flujo de trabajo de una HU

1. Mover el issue a `In Progress` y asignárselo.
2. `git switch main && git pull && git switch -c feat/HU-05-postulacion`.
3. Backend: comando/consulta, validador, handler, endpoint y prueba.
4. Frontend: tipos de `shared/api/types.ts`, esquema Zod, pantalla.
5. Probar localmente con `docker compose up` (API + BD) y `npm run dev` (frontend).
6. PR con `Closes #<n>` → revisión → *Squash and merge* → se despliega solo → probar en producción.

### 3.4 Migraciones sin conflictos

- Antes de crear una migración: `git pull` de `main`.
- Nombre descriptivo: `dotnet ef migrations add AgregarCurriculums`.
- Nunca editar una migración que ya está en `main`.
- Si dos PR traen migraciones, el segundo en fusionarse **borra la suya, actualiza su rama con `main` y la vuelve a generar**.

---

## 4. Fase 2 — Backend transaccional (25 %)

**Lineamientos:** Clean Architecture estricta (Domain, Application, Infrastructure, API) · EF Core Code-First · `HasQueryFilter` por `EmpresaId` · JWT con el `EmpresaId` cifrado como claim · FluentValidation con errores propagados por un middleware global.

### 4.1 Crear la solución (T-01)

Desde la raíz del repositorio:

```bash
mkdir backend && cd backend
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
dotnet new sln -n UPractice

dotnet new classlib -n UPractice.Domain         -o src/UPractice.Domain
dotnet new classlib -n UPractice.Application    -o src/UPractice.Application
dotnet new classlib -n UPractice.Infrastructure -o src/UPractice.Infrastructure
dotnet new webapi   -n UPractice.Api            -o src/UPractice.Api --use-controllers
dotnet new xunit    -n UPractice.Tests          -o tests/UPractice.Tests

dotnet sln add src/UPractice.Domain/UPractice.Domain.csproj \
               src/UPractice.Application/UPractice.Application.csproj \
               src/UPractice.Infrastructure/UPractice.Infrastructure.csproj \
               src/UPractice.Api/UPractice.Api.csproj \
               tests/UPractice.Tests/UPractice.Tests.csproj

dotnet add src/UPractice.Application/UPractice.Application.csproj       reference src/UPractice.Domain/UPractice.Domain.csproj
dotnet add src/UPractice.Infrastructure/UPractice.Infrastructure.csproj reference src/UPractice.Application/UPractice.Application.csproj
dotnet add src/UPractice.Api/UPractice.Api.csproj                       reference src/UPractice.Infrastructure/UPractice.Infrastructure.csproj
dotnet add tests/UPractice.Tests/UPractice.Tests.csproj                 reference src/UPractice.Api/UPractice.Api.csproj

dotnet new tool-manifest
dotnet tool install dotnet-ef
```

- `global.json` fija la versión del SDK (**.NET 10, versión LTS**) para que todos compilen igual. Si alguien tiene otra versión de .NET 10, ajusten el número al SDK que tengan (`dotnet --list-sdks`).
- El *tool manifest* deja `dotnet-ef` como herramienta local del repositorio: cada integrante solo ejecuta `dotnet tool restore`.
- Abrir `backend/UPractice.sln` (o `.slnx`) en JetBrains Rider.

**Regla de dependencias** (se revisa en cada PR): `Domain` no referencia nada. `Application` solo a `Domain`. `Infrastructure` implementa las interfaces de `Application`. `Api` solo arma la aplicación y expone endpoints.

### 4.2 Paquetes

| Proyecto | Paquetes |
|---|---|
| Domain | — |
| Application | `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` |
| Infrastructure | `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.IdentityModel.JsonWebTokens`, `Microsoft.Extensions.Identity.Core` (para `PasswordHasher<T>`) |
| Api | `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore` |
| Tests | `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql` |

> **No usar MediatR ni FluentAssertions:** sus versiones recientes tienen licencia comercial. Los *handlers* se inyectan directamente, y para las pruebas basta con `Assert` de xUnit.

### 4.3 Estructura por funcionalidad (Vertical Slicing en el backend)

```
UPractice.Domain/
  Entidades/        Empresa.cs, Vacante.cs, Postulacion.cs, …
  Enums/            Rol.cs, EstadoVacante.cs, EstadoPostulacion.cs, …
  Reglas/           TransicionesPostulacion.cs   ← máquina de estados (S3.6 del diseño)
  Excepciones/      ReglaNegocioException.cs (con Code), NoEncontradoException.cs, ConflictoException.cs

UPractice.Application/
  Abstracciones/    ICurrentUser.cs, IAppDbContext.cs, IAlmacenamientoArchivos.cs, IGeneradorToken.cs, IFechaHonduras.cs
  Features/
    Vacantes/
      Crear/        CrearVacanteCommand.cs · CrearVacanteValidator.cs · CrearVacanteHandler.cs
      Listar/       ListarVacantesQuery.cs · ListarVacantesHandler.cs
      CambiarEstado/…
    Postulaciones/…
    Auth/…  Perfil/…  Curriculums/…  Coordinacion/…  Admin/…

UPractice.Infrastructure/
  Persistencia/     AppDbContext.cs · Configuraciones/*.cs · Migraciones/ · Seed/
  Auth/             GeneradorJwt.cs · CurrentUser.cs
  Almacenamiento/   LocalStorage.cs · SupabaseStorage.cs

UPractice.Api/
  Controllers/      VacantesController.cs, …   ← delgados: reciben DTO, llaman al handler
  Middleware/       ManejadorErroresMiddleware.cs
  Filtros/          ValidacionFilter.cs
  Program.cs
```

### 4.4 Persistencia y aislamiento (T-02, T-03)

1. Entidades en `Domain` con las columnas del ERD (S3.4 del diseño).
2. Una clase `IEntityTypeConfiguration<T>` por tabla con llaves, índices, `CHECK`, FK compuestas y `HasConversion<string>()` para los enums (S3.5 del diseño).
3. Filtros globales tal como S3.3 del diseño.
4. Primera migración:

```bash
dotnet ef migrations add Inicial -p src/UPractice.Infrastructure -s src/UPractice.Api -o Persistencia/Migraciones
```

5. Migraciones y seed al arrancar, controlados por configuración (sirve igual en Docker y en Render):

```csharp
// Program.cs
if (app.Configuration.GetValue<bool>("Db:AplicarMigraciones"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<Seeder>().EjecutarAsync(); // SuperAdmin + demo si Seed:Demo=true
}
```

6. **Prueba de aislamiento obligatoria** (`tests/UPractice.Tests/AislamientoTests.cs`) con PostgreSQL real en Testcontainers (usa Docker Desktop):
   - Un reclutador de la empresa A **no puede** listar, leer por id, editar ni cambiar el estado de vacantes o postulaciones de la empresa B (espera 404).
   - Un estudiante solo ve vacantes publicadas de empresas aprobadas en su universidad.
   - Un coordinador solo ve solicitudes de habilitación y perfiles de su universidad.

### 4.5 Seguridad (T-03)

- Contraseñas con `PasswordHasher<Usuario>`.
- Token firmado y cifrado (JWE) con el código de S5.2 del diseño. Claves en variables de entorno, generadas con Node:

```bash
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"   # Jwt__ClaveFirma
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"   # Jwt__ClaveCifrado
```

- Política de autorización por rol (`[Authorize(Roles = "Reclutador,AdminEmpresa")]`, etc.).
- Filtro global que devuelve 403 `CAMBIO_PASSWORD_REQUERIDO` si el token trae `pwd_temp` y el endpoint no es `/auth/me` ni `/auth/password`.
- Los usuarios desactivados o de empresas suspendidas no pueden iniciar sesión.

**Evidencia para la presentación:** pegar un token en jwt.io y mostrar que **no se puede leer** (está cifrado).

### 4.6 Validación y errores (T-04)

- **FluentValidation** valida cada *Command*, con los mensajes exactos de la tabla S5.7 del diseño. Las comprobaciones contra la BD (carrera existe y está activa, universidad activa) van en el mismo validador con `MustAsync`.
- Un **filtro global** ejecuta el validador de cada argumento antes de entrar al controlador:

```csharp
public sealed class ValidacionFilter(IServiceProvider sp) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        foreach (var arg in ctx.ActionArguments.Values)
        {
            if (arg is null) continue;
            var tipo = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (sp.GetService(tipo) is IValidator validador)
            {
                var r = await validador.ValidateAsync(new ValidationContext<object>(arg), ctx.HttpContext.RequestAborted);
                if (!r.IsValid) throw new ValidationException(r.Errors);
            }
        }
        await next();
    }
}
```

- El **middleware global** convierte excepciones en ProblemDetails (S5.3 del diseño):

| Excepción | HTTP | Contenido |
|---|---|---|
| `ValidationException` | 400 | `errors` agrupados por campo en camelCase |
| `NoEncontradoException` | 404 | `detail` |
| `ConflictoException` | 409 | `detail` + `code` |
| `ReglaNegocioException` | 422 | `detail` + `code` |
| Cualquier otra | 500 | Mensaje genérico. **Sin stack trace en producción**; se registra en el log con el `traceId`. |

- Nombres de campo en camelCase:

```csharp
ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
    member is null ? null : char.ToLowerInvariant(member.Name[0]) + member.Name[1..];
```

### 4.7 Almacenamiento (T-08)

Interfaz e implementaciones según S3.8 del diseño. Para Supabase Storage basta un `HttpClient` contra su API REST:

| Acción | Petición |
|---|---|
| Subir | `POST {SUPABASE_URL}/storage/v1/object/upractice-archivos/{key}` con el PDF en el cuerpo y `Content-Type: application/pdf` |
| URL firmada | `POST {SUPABASE_URL}/storage/v1/object/sign/upractice-archivos/{key}` con `{ "expiresIn": 300 }` → devuelve una ruta firmada que se antepone con `{SUPABASE_URL}/storage/v1` |
| Eliminar | `DELETE {SUPABASE_URL}/storage/v1/object/upractice-archivos/{key}` |

Encabezados: `apikey` y `Authorization: Bearer` con la clave secreta del proyecto. Confirmen los detalles en la documentación de Supabase Storage al implementarlo.

### 4.8 Swagger

- Swagger UI solo en `Development` (o protegido en producción).
- Configurar el botón **Authorize** con esquema Bearer para probar los endpoints con token.
- Swagger es el contrato vivo: si difiere de S5.5 del diseño, se corrige uno de los dos en el mismo PR.

### 4.9 Lista de cierre de la Fase 2

- [ ] Cuatro proyectos con dependencias en la dirección correcta.
- [ ] Code-First: el esquema sale de las migraciones, nadie crea tablas a mano.
- [ ] `HasQueryFilter` en todas las tablas transaccionales; prueba de aislamiento en verde.
- [ ] JWT cifrado con claim `empresa_id`; Bearer funcionando en Swagger.
- [ ] Todos los *Commands* tienen validador; los errores salen como ProblemDetails desde el middleware.
- [ ] Endpoints del Sprint 1 implementados según S5.5 del diseño.

---

## 5. Fase 3 — Frontend web reactivo (25 %)

**Lineamientos:** Vertical Slicing (`features/...`) · rutas protegidas con `react-router-dom` · Zod + React Hook Form con `mode: 'onChange'` simétricos a FluentValidation · extractor universal de errores (el de la Clase 16) e interceptores de Axios con Bearer.

### 5.1 Crear el proyecto (T-05)

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
npm install react-router-dom axios react-hook-form zod @hookform/resolvers @tanstack/react-query
npm install @mui/material @emotion/react @emotion/styled
```

- **MUI** se recomienda porque trae tablas, formularios, diálogos y fechas listos; elijan otra librería solo si alguien ya la domina. Lo importante es usar **una sola**.
- **TanStack Query** maneja carga, caché y recarga de datos; reduce mucho código repetido en listados.
- `.env.development`: `VITE_API_URL=http://localhost:8080/api/v1` (y `example.env` versionado con la misma variable vacía).

### 5.2 Estructura

```
frontend/src/
  app/
    router.tsx            ← todas las rutas, agrupadas por rol
    providers.tsx         ← QueryClientProvider, AuthProvider, tema MUI
  shared/
    api/
      http.ts             ← instancia de Axios + interceptores
      types.ts            ← DTO de S5.6 del diseño (copiados tal cual)
      errores.ts          ← extractor universal de errores
    auth/
      AuthContext.tsx     ← usuario actual, login, logout
      RutaProtegida.tsx   ← exige sesión
      RutaPorRol.tsx      ← exige rol
    lib/fechas.ts         ← hoyHonduras()
    components/           ← Layout, tablas, mensajes, ConfirmDialog…
  features/
    auth/          pages/ · schemas/ · api.ts
    perfil/        …
    curriculums/   …
    vacantes/      …
    postulaciones/ …
    empresa/       (usuarios, habilitaciones)
    coordinacion/  …
    admin/         …
```

Cada `feature` tiene sus `pages/`, `components/`, `schemas/` (Zod) y `api.ts` (llamadas con `http`). Una feature **no importa** archivos internos de otra; lo compartido va en `shared/`.

### 5.3 Axios e interceptores

```ts
// shared/api/http.ts
export const http = axios.create({ baseURL: import.meta.env.VITE_API_URL });

http.interceptors.request.use((config) => {
  const token = sesion.obtenerToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

http.interceptors.response.use(
  (r) => r,
  (error) => {
    const status = error.response?.status;
    const esLogin = error.config?.url?.includes('/auth/login');
    if (status === 401 && !esLogin) {
      sesion.cerrar();
      window.location.assign('/login');
    }
    if (status === 403 && error.response?.data?.code === 'CAMBIO_PASSWORD_REQUERIDO') {
      window.location.assign('/cambiar-password');
    }
    return Promise.reject(error);
  },
);
```

- El token se guarda en `localStorage` (aceptable para el proyecto) y **nunca se decodifica**: está cifrado. Los datos del usuario vienen de `LoginResponse.usuario` y de `GET /auth/me` al recargar la página.

### 5.4 Extractor universal de errores

Adaptar el extractor de la Clase 16 para que lea el formato ProblemDetails de S5.3 del diseño y devuelva siempre la misma forma:

```ts
// shared/api/errores.ts
export interface ErrorApi {
  mensaje: string;                         // para un Snackbar o Alert
  campos: Record<string, string>;          // errores por campo (400)
  code?: string;                           // regla de negocio (409/422)
  status?: number;
}

export function extraerError(error: unknown): ErrorApi { /* … */ }

export function aplicarErrores<T extends FieldValues>(error: ErrorApi, setError: UseFormSetError<T>) {
  Object.entries(error.campos).forEach(([campo, mensaje]) =>
    setError(campo as Path<T>, { type: 'server', message: mensaje }));
}
```

Debe manejar: sin conexión o API dormida, 400 con `errors`, 401, 403, 404, 409/422 con `code`, y 500.

### 5.5 Formularios simétricos

- Un esquema Zod por formulario en `features/<x>/schemas/`, con **los mismos límites y mensajes** que el validador del backend (S5.7 del diseño).
- `useForm({ resolver: zodResolver(esquema), mode: 'onChange' })`.
- Inputs numéricos con `register('cupos', { valueAsNumber: true })`.
- Al recibir 400 del backend, `aplicarErrores(...)` pinta el error en el mismo campo.
- **Prueba de simetría:** desactivar temporalmente la validación de Zod de un formulario, enviar datos inválidos y comprobar que el backend devuelve **el mismo mensaje en el mismo campo**. Sirve como evidencia en la presentación.

### 5.6 Rutas y pantallas

| Rol | Ruta | Pantalla | HU |
|---|---|---|---|
| Público | `/login` | Inicio de sesión | HU-01 |
| Público | `/registro/estudiante`, `/registro/empresa` | Registros | HU-01, HU-E01 |
| Autenticado | `/cambiar-password` | Cambio de contraseña obligatorio | D-17 |
| Estudiante | `/estudiante/perfil` | Perfil académico, habilidades, privacidad | HU-02 |
| Estudiante | `/estudiante/curriculums` | Versiones de CV | HU-03 |
| Estudiante | `/vacantes`, `/vacantes/:id` | Explorar y detalle (con botón Postular) | HU-04, HU-05 |
| Estudiante | `/estudiante/postulaciones`, `/estudiante/postulaciones/:id` | Seguimiento, retirar, responder oferta | HU-06 |
| Reclutador / AdminEmpresa | `/empresa/vacantes`, `/empresa/vacantes/nueva`, `/empresa/vacantes/:id/editar` | Panel y formulario de vacantes | HU-E03, HU-E04 |
| Reclutador / AdminEmpresa | `/empresa/vacantes/:id/postulantes`, `/empresa/postulaciones/:id` | Postulantes, perfil, CV, cambio de estado | HU-E05, HU-E06, HU-E07 |
| AdminEmpresa | `/empresa/usuarios`, `/empresa/habilitaciones` | Reclutadores y solicitudes | HU-E02, HU-E01 |
| Coordinador | `/coordinacion/empresas`, `/coordinacion/estudiantes` | Aprobar empresas, verificar perfiles | HU-C01, HU-C02 |
| SuperAdmin | `/admin/universidades`, `/admin/universidades/:id/catalogo`, `/admin/habilidades`, `/admin/coordinadores`, `/admin/empresas` | Administración de la plataforma | HU-A01 a HU-A04 |

Guardias:
- `RutaProtegida`: sin sesión → `/login`; con `debeCambiarPassword` → `/cambiar-password`.
- `RutaPorRol roles={[...]}`: rol no permitido → página "Sin acceso".
- Después del login, cada rol aterriza en su pantalla principal.

### 5.7 Lista de cierre de la Fase 3

- [ ] Carpetas por funcionalidad en `src/features/`.
- [ ] Ninguna ruta privada se abre sin token (probar escribiendo la URL directamente).
- [ ] Todos los formularios usan Zod + React Hook Form con `mode: 'onChange'` y mensajes idénticos al backend.
- [ ] Interceptor de request agrega `Bearer`; interceptor de response maneja 401 y 403.
- [ ] Todos los errores de la API pasan por el extractor universal.
- [ ] Estados de carga y lista vacía en cada listado.

---

## 6. Fase 4 — Contenerización (15 %)

**Lineamientos:** Dockerfile multi-stage para la API (SDK para compilar, runtime ligero de Alpine para ejecutar) · `docker-compose.yml` que levante la API conectada a un PostgreSQL efímero.

### 6.1 `backend/Dockerfile` (T-06)

```dockerfile
# ---------- Etapa 1: compilar ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Primero solo los .csproj: Docker reutiliza el restore si no cambian
COPY global.json ./
COPY src/UPractice.Domain/UPractice.Domain.csproj                 src/UPractice.Domain/
COPY src/UPractice.Application/UPractice.Application.csproj       src/UPractice.Application/
COPY src/UPractice.Infrastructure/UPractice.Infrastructure.csproj src/UPractice.Infrastructure/
COPY src/UPractice.Api/UPractice.Api.csproj                       src/UPractice.Api/
RUN dotnet restore src/UPractice.Api/UPractice.Api.csproj

COPY src/ src/
RUN dotnet publish src/UPractice.Api/UPractice.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Etapa 2: ejecutar ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/storage && chown "$APP_UID" /app/storage
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "UPractice.Api.dll"]
```

- `tzdata` es necesaria para calcular la fecha de Honduras (`America/Tegucigalpa`); `icu-libs` para formatos de cultura.
- La imagen corre con un usuario sin privilegios (`$APP_UID` viene definido en las imágenes oficiales de .NET).
- Si agregan `Directory.Build.props` o `Directory.Packages.props`, también hay que copiarlos antes del `restore`.

### 6.2 `backend/.dockerignore`

```
**/bin/
**/obj/
**/.vs/
**/.idea/
tests/
storage/
*.user
```

### 6.3 `docker-compose.yml` (raíz del repositorio)

```yaml
services:
  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: upractice
      POSTGRES_USER: upractice
      POSTGRES_PASSWORD: upractice_dev
    ports:
      - "5433:5432"                 # 5433 para no chocar con un PostgreSQL local
    tmpfs:
      - /var/lib/postgresql/data    # efímero: los datos se borran al detener el contenedor
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U upractice -d upractice"]
      interval: 5s
      timeout: 3s
      retries: 10

  api:
    build:
      context: ./backend
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: "Host=db;Port=5432;Database=upractice;Username=upractice;Password=upractice_dev"
      Db__AplicarMigraciones: "true"
      Seed__Demo: "true"
      Seed__SuperAdminCorreo: "admin@upractice.local"
      Seed__SuperAdminPassword: ${SUPERADMIN_PASSWORD}
      Jwt__Issuer: "upractice-api"
      Jwt__Audience: "upractice-web"
      Jwt__ClaveFirma: ${JWT_CLAVE_FIRMA}
      Jwt__ClaveCifrado: ${JWT_CLAVE_CIFRADO}
      Storage__Proveedor: "Local"
      Storage__RutaLocal: "/app/storage"
      Cors__Origenes: "http://localhost:5173"
    volumes:
      - upractice-storage:/app/storage

volumes:
  upractice-storage:
```

`example.env` (versionado) y `.env` (cada uno el suyo, **no se sube**):

```dotenv
SUPERADMIN_PASSWORD=
JWT_CLAVE_FIRMA=
JWT_CLAVE_CIFRADO=
```

### 6.4 Uso diario

```bash
docker compose up --build        # levanta BD + API (migra y carga datos demo)
docker compose down              # detiene todo; la BD se borra (es efímera)
docker compose logs -f api       # ver logs de la API
```

- Swagger: `http://localhost:8080/swagger`.
- Frontend aparte: `cd frontend && npm run dev` → `http://localhost:5173`.
- Para ver la BD desde el IDE de JetBrains: *Database → + → PostgreSQL*, host `localhost`, puerto `5433`, usuario y contraseña `upractice` / `upractice_dev`.
- Para depurar la API con breakpoints desde Rider: levantar solo la BD (`docker compose up db`) y ejecutar la API desde el IDE con la cadena de conexión a `localhost:5433`.

### 6.5 Lista de cierre de la Fase 4

- [ ] `docker build` produce una imagen basada en `aspnet:10.0-alpine` (verificar tamaño con `docker images`).
- [ ] Un integrante que **nunca** ha ejecutado el proyecto clona el repo, copia `example.env` a `.env`, ejecuta `docker compose up --build` y entra a Swagger sin pasos extra.
- [ ] Al hacer `down` y `up` la BD vuelve a crearse con los datos demo.

---

## 7. Fase 5 — Despliegue cloud automatizado (20 %)

**Lineamientos:** PostgreSQL en Neon o Supabase · API en Render o Fly.io compilando el Dockerfile con HTTPS · frontend en Vercel o Netlify desde `main` · cada `git push` a `main` actualiza todo.

Se usan **Supabase + Render + Vercel** (D-18 del diseño). Los tres se crean con la cuenta de GitHub y no piden tarjeta.

### 7.1 Supabase (BD y archivos) — lun 9

1. **New project** → nombre `upractice`, región **East US (North Virginia)**. Guardar la contraseña de la BD en un lugar seguro del equipo.
2. **Storage → New bucket** → `upractice-archivos`, **privado**. Límite de tamaño 5 MB y tipo permitido `application/pdf`.
3. **Connect → Session pooler**: copiar los datos de conexión. Usar el *pooler* y no la conexión directa, porque esta última requiere IPv6 y Render no lo usa. Formato para .NET:

```
Host=<host-del-pooler>;Port=5432;Database=postgres;Username=postgres.<ref-del-proyecto>;Password=<contraseña>;SSL Mode=Require;Maximum Pool Size=10
```

4. **Project Settings → API Keys**: copiar la URL del proyecto y la **clave secreta** (*secret* / *service_role*). Solo va en Render.

Límites del plan gratuito: 500 MB de BD, 1 GB de archivos, 2 proyectos activos por cuenta, y **el proyecto se pausa tras 7 días sin actividad** (se reactiva manualmente desde el panel).

### 7.2 Render (API) — lun 9 a mié 11

1. **New → Web Service** → conectar GitHub → repositorio `upractice`.
2. Configuración:

| Campo | Valor |
|---|---|
| Name | `upractice-api` (la URL será `https://upractice-api.onrender.com` si el nombre está libre) |
| Language | Docker |
| Branch | `main` |
| Root Directory | `backend` |
| Region | Virginia (US East), cerca de Supabase |
| Instance Type | Free |
| Health Check Path | `/health` |
| Auto-Deploy | On Commit (o *After CI checks pass* si está disponible, después del paso 7.5) |

3. Variables de entorno:

| Variable | Valor |
|---|---|
| `PORT` | `8080` (Render dirige el tráfico a este puerto; por defecto espera 10000) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | Cadena del Session pooler (7.1) |
| `Db__AplicarMigraciones` | `true` |
| `Seed__Demo` | `true` hasta la presentación (datos de demostración); luego `false` |
| `Seed__SuperAdminCorreo` / `Seed__SuperAdminPassword` | Cuenta inicial (contraseña fuerte) |
| `Jwt__Issuer` / `Jwt__Audience` | `upractice-api` / `upractice-web` |
| `Jwt__ClaveFirma` / `Jwt__ClaveCifrado` | **Claves nuevas**, distintas a las de desarrollo |
| `Storage__Proveedor` | `Supabase` |
| `Storage__SupabaseUrl` / `Storage__SupabaseClave` / `Storage__Bucket` | Datos de 7.1 / `upractice-archivos` |
| `Cors__Origenes` | URL de producción de Vercel (7.3), sin `/` final |

4. El plan gratuito **apaga el servicio tras unos minutos sin tráfico**; la primera petición después tarda cerca de un minuto. No es un error, pero hay que tenerlo en cuenta en la demo (S9).

### 7.3 Vercel (frontend) — mié 11

1. **Add New → Project** → importar `upractice`.
2. **Root Directory:** `frontend`. **Framework Preset:** Vite (lo detecta solo).
3. **Environment Variables:** `VITE_API_URL=https://upractice-api.onrender.com/api/v1`.
4. **Deploy**. La rama de producción es `main` por defecto.
5. Agregar `frontend/vercel.json` para que las rutas de React funcionen al recargar la página:

```json
{
  "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }]
}
```

6. Copiar la URL de producción a `Cors__Origenes` en Render.

> Vercel crea además un enlace de vista previa por cada PR. Esos enlaces no estarán en CORS: para probar un PR, usen el entorno local.

### 7.4 Endpoint de salud

```csharp
app.MapGet("/health", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();
```

### 7.5 Integración continua (GitHub Actions) — mié 11

`.github/workflows/ci.yml`:

```yaml
name: CI
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: backend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: backend/global.json
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release

  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: npm
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run lint --if-present
      - run: npm run build

  docker:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: docker build -t upractice-api ./backend
```

Después de que el workflow corra una vez:
1. En el repositorio: **Settings → Rules → Rulesets → proteger-main → Require status checks to pass** → agregar `backend`, `frontend` y `docker`.
2. En Render, si existe la opción, cambiar *Auto-Deploy* a **After CI checks pass**.

Resultado: un PR no se puede fusionar si algo no compila, y cada fusión a `main` despliega API y frontend automáticamente.

### 7.6 Verificación después de cada despliegue

- [ ] `https://upractice-api.onrender.com/health` responde `{"estado":"ok"}`.
- [ ] El login funciona desde la URL de Vercel (si falla con error de CORS, revisar `Cors__Origenes`).
- [ ] Recargar una ruta interna (por ejemplo, `/empresa/vacantes`) no da 404.
- [ ] Subir y descargar un CV funciona (Supabase Storage).
- [ ] La pestaña **Actions** de GitHub está en verde y el *Events* de Render muestra el despliegue del último commit.

### 7.7 Lista de cierre de la Fase 5

- [ ] BD en Supabase creada por migraciones (no a mano).
- [ ] API en Render construida desde el Dockerfile, con HTTPS.
- [ ] Frontend en Vercel desde `main`.
- [ ] Un `git push` a `main` actualiza API y frontend sin pasos manuales (demostrable en vivo, S9).
- [ ] Ningún secreto en el repositorio (revisar el historial con la búsqueda de GitHub: `ClaveFirma`, `Password=`).

---

## 8. Riesgos y plan de recorte

| Riesgo | Señal | Respuesta |
|---|---|---|
| Atraso en el Sprint 1 | En el hito H1 (mié 18) no está el flujo vacante → estudiante | Aplicar el recorte (abajo) de inmediato, no esperar al 25. |
| Problemas de despliegue al final | — | Ya mitigado: el esqueleto se despliega el mié 11 y cada PR se despliega. |
| Conflictos de migraciones | PR con migración que no aplica | Regla de S3.4: el segundo PR regenera su migración. |
| Render dormido en la presentación | Primera carga de ~1 min | Abrir `/health` y hacer login 5 minutos antes de presentar. |
| Supabase pausado | Panel indica *Paused* | Usar el sistema al menos una vez por semana; revisar el panel el día antes. |
| Un integrante no puede trabajar unos días | Su columna del tablero no se mueve en 2 días | Reasignar en la reunión siguiente; las HU son independientes gracias al seed. |
| Falla de internet o servicio caído el día de la demo | — | Tener `docker compose up` listo en una laptop como respaldo y un video corto grabado del flujo completo. |

**Recorte, en este orden** (solo si hace falta; nunca recortar lo que piden los lineamientos):

1. HU-A04 (suspender empresas).
2. HU-E02 (múltiples reclutadores): la empresa trabaja solo con su `AdminEmpresa`.
3. HU-03 reducida a un único CV por estudiante.
4. HU-A02 sin pantalla: carreras y habilidades solo por seed.
5. Filtros avanzados de HU-04: quedan solo búsqueda por texto y carrera.

---

## 9. Preparación de la presentación

### 9.1 Datos de demostración en producción

El seed de demo debe dejar listo:
- 2 universidades (UNAH y UNITEC) con 2–3 facultades y varias carreras.
- 1 coordinador por universidad.
- 2 empresas aprobadas con vacantes publicadas, y 1 empresa pendiente de aprobación.
- 3–4 estudiantes verificados y 1 pendiente, con postulaciones en distintos estados.
- Una hoja (fuera del repositorio) con las credenciales de cada cuenta demo.

### 9.2 Guion sugerido (15 minutos)

| Min | Quién | Qué se muestra | Lineamiento que demuestra |
|---|---|---|---|
| 0–2 | B | Pitch: problema, solución, actores, modelo SaaS | Fase 1 |
| 2–3 | B | Tablero Kanban, una HU con sus criterios, ERD | Fase 1 |
| 3–5 | A | Empresa nueva se registra → coordinador la aprueba → publica vacante | Fase 2 y 3 |
| 5–7 | D | Estudiante explora, se postula con CV; empresa lo mueve a *Entrevista* y *Oferta*; estudiante acepta | Fase 2 y 3 |
| 7–8 | A | **Aislamiento:** reclutador de la empresa A intenta abrir una postulación de la empresa B por URL → 404. Prueba automática en verde. | Fase 2 (`HasQueryFilter`) |
| 8–9 | A | **Token cifrado:** pegarlo en jwt.io → no se puede leer | Fase 2 (JWT) |
| 9–10 | C | **Validación simétrica:** error en vivo con Zod; mismo mensaje devuelto por FluentValidation | Fase 2 y 3 |
| 10–11 | C | Ruta protegida sin sesión; 401 → regreso al login (interceptor) | Fase 3 |
| 11–12 | D | Dockerfile multi-stage y `docker compose up` | Fase 4 |
| 12–14 | D | Cambio visible pequeño → `git push` → Actions → Render y Vercel se actualizan solos | Fase 5 |
| 14–15 | Todos | Visión futura (Sprint 2 y 3) y preguntas | — |

> Para el paso 12–14, dejar preparado de antemano un cambio de una línea (por ejemplo, el texto del pie de página) en una rama con su PR ya aprobado; en la demo solo se fusiona. El despliegue completo tarda unos minutos, así que se puede hacer al inicio de ese bloque y mostrar el resultado al final.

### 9.3 Día de la presentación

- [ ] 30 min antes: abrir `/health`, iniciar sesión con cada rol (despierta Render y verifica todo).
- [ ] Pestañas abiertas en orden: pitch, tablero, app en Vercel (una ventana por rol, en ventanas privadas), jwt.io, GitHub Actions, Render.
- [ ] Laptop de respaldo con `docker compose up` funcionando y el video de respaldo.
- [ ] Nadie fusiona nada a `main` el día de la presentación, salvo el cambio preparado para la demo.

---

## 10. Lista final por lineamiento

| Fase (peso) | Requisito | Dónde se cumple | Listo |
|---|---|---|---|
| **1 (15 %)** | Pitch de 1 página | `docs/fase-1/pitch.pdf` | [ ] |
| | ERD 3FN con `EmpresaId` | Diseño S3, `erd.png` | [ ] |
| | Kanban con HU claras | GitHub Projects | [ ] |
| | Contratos de interfaz | Diseño S5 | [ ] |
| **2 (25 %)** | Clean Architecture estricta | `backend/src/*` (S4.1) | [ ] |
| | EF Core Code-First + `HasQueryFilter` por `EmpresaId` | `AppDbContext`, migraciones, prueba de aislamiento | [ ] |
| | JWT con `EmpresaId` cifrado | `GeneradorJwt` (JWE) | [ ] |
| | FluentValidation + middleware global | `ValidacionFilter`, `ManejadorErroresMiddleware` | [ ] |
| **3 (25 %)** | Vertical Slicing | `frontend/src/features/*` | [ ] |
| | Rutas protegidas con `react-router-dom` | `RutaProtegida`, `RutaPorRol` | [ ] |
| | Zod + RHF `onChange` simétrico | `features/*/schemas` | [ ] |
| | Extractor universal + interceptores Axios | `shared/api/errores.ts`, `shared/api/http.ts` | [ ] |
| **4 (15 %)** | Dockerfile multi-stage SDK → Alpine | `backend/Dockerfile` | [ ] |
| | Compose con API + PostgreSQL efímero | `docker-compose.yml` | [ ] |
| **5 (20 %)** | BD en Supabase | Proyecto `upractice` | [ ] |
| | API en Render desde Dockerfile con HTTPS | `upractice-api.onrender.com` | [ ] |
| | Frontend en Vercel desde `main` | URL de Vercel | [ ] |
| | Push a `main` actualiza todo | Auto-deploy + GitHub Actions | [ ] |
