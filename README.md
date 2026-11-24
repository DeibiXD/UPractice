# UPractice

Plataforma SaaS B2B que conecta empresas, estudiantes próximos a graduarse y coordinaciones académicas para gestionar la práctica profesional de punta a punta.

Proyecto final · Plataforma Web SaaS Multi-empresa.

## Documentación

| Documento | Contenido |
|---|---|
| [Diseño de la Fase 1](docs/FASES/UPractice-Fase1.md) | Roles, historias, reglas y diseño inicial. Esta copia contiene D-01 a D-18; para el modelo y los DTO actuales, consultar el ERD y los contratos vigentes. |
| [Pitch](<docs/Documentos - UPractice/pitch.pdf>) | Pitch de una página. |
| [ERD v4 (Mermaid)](docs/diagrama-mermaid.md) · [imagen](<docs/diagrama 4.png>) | Diagrama entidad-relación en 3FN. La fuente es el Mermaid; la imagen se exporta desde mermaid.live. |
| [Contratos TypeScript](docs/contratos/api-types.ts) | DTO de S5.6, listos para `frontend/src/shared/api/types.ts`. |
| [Mensajes de validación](docs/mensajes-validacion.md) | Textos idénticos para FluentValidation y Zod. |
| [Lineamientos del curso](docs/FASES/Lineamientos-del-curso.md) | Requisitos oficiales. Si algo choca con ellos, mandan los lineamientos. |
| [Tablero Kanban](https://github.com/users/DeibiXD/projects/3) | Historias de usuario y tareas por sprint. |

## Stack
- Backend: .NET 10 + Entity Framework Core + PostgreSQL (Clean Architecture)
- Frontend: React + TypeScript + Vite
- Contenedores: Docker / Docker Compose
- Nube: Supabase (BD y archivos) · Render (API) · Vercel (frontend)

## Estructura

```
backend/    Solución .NET (UPractice.slnx): Domain, Application, Infrastructure, Api y Tests
frontend/   SPA React (se crea en T-05)
docs/       Diseño, ERD, contratos y mensajes de validación
```

## Correr con Docker (T-06)

Requiere Docker Desktop iniciado con contenedores Linux. Desde la raíz del repositorio,
la primera vez copia la plantilla si todavía no tienes un `.env` propio:

```powershell
if (-not (Test-Path -LiteralPath .env)) {
    Copy-Item -LiteralPath .env.example -Destination .env
}
```

Abre `.env` y completa `POSTGRES_PASSWORD`, `SUPERADMIN_PASSWORD` y `DEMO_PASSWORD`
con valores locales distintos. Compose verifica que estén completos antes de arrancar.
Las dos claves JWT pueden quedar vacías en este entorno Development: la API las genera
temporalmente y tendrás que iniciar sesión otra vez después de reiniciarla.
Para conservar los tokens entre reinicios, completa ambas claves según la plantilla.

```powershell
docker compose config --quiet
docker compose up --build
```

Abre **http://localhost:8080/swagger**. La API espera a que PostgreSQL esté disponible,
aplica la migración existente y carga el seed. Puedes iniciar sesión con
`admin@digitales.example.com` y tu `DEMO_PASSWORD`, o con `admin@upractice.app`
y tu `SUPERADMIN_PASSWORD` (esta última cuenta requiere cambiar la contraseña).

La API se publica en `127.0.0.1:8080` y PostgreSQL en `127.0.0.1:5433`;
los puertos se ajustan con `API_PORT` y `POSTGRES_PORT` en `.env`.
Para DBeaver: base `upractice`, usuario `upractice` y contraseña `POSTGRES_PASSWORD`.
El contenedor manual `upractice-pg` de los pasos siguientes utiliza otro puerto y volumen.

`docker compose down` detiene este entorno. **La BD de Compose es temporal (`tmpfs`):
sus datos se pierden al detener el contenedor de PostgreSQL.** En el siguiente arranque
se reconstruyen la BD y los datos demo. El volumen de archivos se conserva;
su proveedor funcional corresponde a T-08, todavía separado de esta entrega.

La [guía de verificación de T-06](docs/pruebas/T-06.md) detalla el arranque,
las pruebas de persistencia y cómo comprobar la ejecución sin privilegios.

## Correr el backend con .NET local

```bash
# 1. PostgreSQL persistente para ejecutar la API con .NET local (una sola vez)
docker run -d --name upractice-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=upractice -p 5432:5432 -v upractice-pgdata:/var/lib/postgresql/data postgres:17-alpine

# 2. Contraseñas del seed (solo en tu máquina, nunca en el repo)
cd backend
dotnet user-secrets set "Seed:SuperAdminPassword" "<elige-una>" --project src/UPractice.Api
dotnet user-secrets set "Seed:DemoPassword" "<elige-una>" --project src/UPractice.Api

# 3. Compilar y correr: aplica la migración y carga el seed al arrancar
dotnet tool restore      # instala dotnet-ef local
dotnet build
dotnet run --project src/UPractice.Api
# GET http://localhost:5294/health → {"estado":"ok"}
```

En desarrollo (`appsettings.Development.json`) el seed carga datos de demostración: UNAH y UNITEC con sus carreras, un coordinador por universidad, dos empresas aprobadas con su administrador y reclutador, estudiantes verificados, vacantes y una postulación. Todas esas cuentas usan `Seed:DemoPassword`; el SuperAdmin es `admin@upractice.app` con `Seed:SuperAdminPassword` y debe cambiar la contraseña en su primer ingreso.

**Probar la API:** abrir `http://localhost:5294/swagger`, ejecutar `POST /api/v1/auth/login`, copiar el `accessToken` y pegarlo en **Authorize**. Con contraseña temporal solo responden `GET /auth/me` y `PUT /auth/password` (las demás rutas dan 403 `CAMBIO_PASSWORD_REQUERIDO`). En desarrollo, si no defines `Jwt:ClaveFirma` y `Jwt:ClaveCifrado`, la API genera claves temporales y los tokens dejan de servir al reiniciarla. En producción son obligatorias (variables `Jwt__ClaveFirma` y `Jwt__ClaveCifrado`, en Base64; se generan con `node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"`).

El catálogo público está en `GET /api/v1/catalogos/universidades`. El registro de estudiantes se prueba en `POST /api/v1/auth/registro/estudiante`; un correo institucional recibe una sesión, los dominios no registrados o inactivos responden `422 CORREO_NO_INSTITUCIONAL` y los correos duplicados `409 CORREO_EN_USO`. La colección `src/UPractice.Api/auth-tests.http` contiene estos flujos y solicitudes para login y `/auth/me`.

**Pruebas:** `dotnet test tests/UPractice.Tests`. Usan Testcontainers (necesitan Docker abierto) o, si existe la variable `PRUEBAS_POSTGRES`, un PostgreSQL existente, donde crean una base temporal y la borran al terminar. Incluyen la prueba obligatoria de aislamiento entre empresas (`AislamientoTests`).

**HU-E03 (backend):** `POST /api/v1/empresa/vacantes` permite crear borradores y publicar con carreras habilitadas para la empresa autenticada. La [guía de prueba](docs/pruebas/HU-E03.md) explica cómo obtener IDs del seed, enviar solicitudes en Swagger y comprobar las respuestas. Los catálogos GET y el panel de HU-E04 se integran por separado.

### Si Windows bloquea la API ("An application control policy has blocked this file")

Smart App Control de Windows 11 bloquea los programas sin firma que compila Rider. Se corre todo dentro de Docker, sin cambiar la seguridad del equipo (PowerShell, desde `backend`):

```powershell
# API (usa tus user-secrets y el contenedor upractice-pg)
docker run --rm -it --name upractice-api -v "${PWD}:/src:ro" -v "$env:APPDATA\Microsoft\UserSecrets:/root/.microsoft/usersecrets:ro" -e ASPNETCORE_ENVIRONMENT=Development -e "ConnectionStrings__Postgres=Host=host.docker.internal;Port=5432;Database=upractice;Username=postgres;Password=postgres" -p 5294:8080 mcr.microsoft.com/dotnet/sdk:10.0 sh -c "cd /src && dotnet run --project src/UPractice.Api --artifacts-path /tmp/a --urls http://0.0.0.0:8080"

# Pruebas
docker run --rm -v "${PWD}:/src:ro" -e "PRUEBAS_POSTGRES=Host=host.docker.internal;Port=5432;Username=postgres;Password=postgres" mcr.microsoft.com/dotnet/sdk:10.0 sh -c "cd /src && dotnet test tests/UPractice.Tests --artifacts-path /tmp/a"
```

Migraciones: solo se crean con `dotnet ef migrations add <Nombre> --project src/UPractice.Infrastructure --startup-project src/UPractice.Api --output-dir Persistencia/Migraciones`, después de hacer `git pull` de `main`.

La ejecución con .NET local requiere el SDK indicado en `backend/global.json`
(`dotnet --list-sdks`). La ejecución con Docker utiliza el SDK dentro de la imagen.

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
5. Commits con Conventional Commits: `feat(postulaciones): validar carrera elegible`.
