UPractice · Fase 2 · Backend (.NET \+ PostgreSQL)

# Qué falta para presentar una Fase 2 completa

Estado al domingo 15 de noviembre de 2026\. La base técnica que piden los lineamientos ya está hecha. Lo que sigue son los endpoints de negocio, para que el aislamiento entre empresas se vea en operaciones reales y no solo en pruebas.

## Lo que ya está

en main · \#39\*\*Base de datos (T-02)\*\*19 tablas Code-First, migración `Inicial`, seed con datos de demo (UNAH, UNITEC, 2 empresas, estudiantes, vacantes).

PR \#40 · por fusionar\*\*Seguridad (T-03)\*\*Token JWT firmado y cifrado, `ICurrentUser`, filtros `HasQueryFilter` por `EmpresaId`, login, `/auth/me`, cambio de contraseña.

PR \#40 · por fusionar\*\*Errores y validación (T-04)\*\*Middleware global ProblemDetails, FluentValidation automático, Swagger con *Authorize*.

Los cuatro requisitos de los lineamientos de la Fase 2 (capas, filtros globales, `EmpresaId` encriptado en el token y FluentValidation con middleware) se cumplen una vez fusionado el PR \#40.

# Todo trabajo nuevo empieza desde `main` actualizado, después de fusionar el PR \#40.

## Paquetes de trabajo

Cada paquete es la parte de **backend** de varias historias de usuario que van juntas: endpoints, validadores y pruebas. Los paquetes son independientes, así que cada quien puede tomar uno sin esperar a los demás. La pantalla de cada HU es de la Fase 3\.

### 1 · Archivos y CV

Ronald · domingo 15

- T-08: `IAlmacenamientoArchivos` local y Supabase, con URL firmadas de 5 minutos.  
- HU-03: subir, listar, marcar principal, eliminar (baja lógica) y descargar CV.

[\#30 T-08](https://github.com/DeibiXD/UPractice/issues/30)[\#8 HU-03](https://github.com/DeibiXD/UPractice/issues/8)

Necesario para el paquete 4 (el estudiante adjunta un CV al postularse).

### 2 · Registro y administración

**sin asignar**

- Catálogos públicos: universidades, carreras, habilidades.  
- HU-01 registro de estudiante (dominio institucional) y HU-E01 registro de empresa con solicitudes de habilitación.  
- HU-A01 a HU-A04: universidades, facultades y carreras, coordinadores, suspensión de empresas (rol SuperAdmin).

[\#6](https://github.com/DeibiXD/UPractice/issues/6)[\#12](https://github.com/DeibiXD/UPractice/issues/12)[\#20](https://github.com/DeibiXD/UPractice/issues/20)[\#21](https://github.com/DeibiXD/UPractice/issues/21)[\#22](https://github.com/DeibiXD/UPractice/issues/22)[\#23](https://github.com/DeibiXD/UPractice/issues/23)

Es lo primero del flujo: sin registro no hay usuarios nuevos para la demo.

### 3 · Vacantes y empresa

sin asignar

- HU-E03 crear y publicar vacante (exige habilitación aprobada, carreras permitidas).  
- HU-E04 panel de vacantes y cambios de estado (máquina de estados ya hecha en `TransicionesVacante`).  
- HU-04 exploración y filtros para el estudiante.  
- HU-E02 reclutadores de la empresa.

[\#14](https://github.com/DeibiXD/UPractice/issues/14)[\#15](https://github.com/DeibiXD/UPractice/issues/15)[\#9](https://github.com/DeibiXD/UPractice/issues/9)[\#13](https://github.com/DeibiXD/UPractice/issues/13)

El que mejor demuestra el filtro por `EmpresaId` a nivel de aplicativo (en vivo).

### 4 · Postulaciones

sin asignar

- HU-05 postularse (perfil verificado, carrera elegible, `CumpleRequisitos`).  
- HU-06 panel del estudiante: retirar, reactivar, aceptar o declinar oferta.  
- HU-E05, HU-E06, HU-E07: postulantes de una vacante, detalle y cambio de estado (máquina ya hecha en `TransicionesPostulacion`).

[\#10](https://github.com/DeibiXD/UPractice/issues/10)[\#11](https://github.com/DeibiXD/UPractice/issues/11)[\#16](https://github.com/DeibiXD/UPractice/issues/16)[\#17](https://github.com/DeibiXD/UPractice/issues/17)[\#18](https://github.com/DeibiXD/UPractice/issues/18)

Se puede empezar ya: el seed trae vacantes y estudiantes verificados.

### 5 · Coordinación y perfil

sin asignar

- HU-02 perfil académico del estudiante y privacidad (`MostrarIndice`).  
- HU-C01 aprobar o rechazar habilitaciones de empresas.  
- HU-C02 verificar perfiles académicos.

[\#7](https://github.com/DeibiXD/UPractice/issues/7)[\#2](https://github.com/DeibiXD/UPractice/issues/2)[\#19](https://github.com/DeibiXD/UPractice/issues/19)

Paquete corto; lo puede tomar quien termine primero o sumarse al 2\.

**Prioridad para demostración en vivo(recordar que a nivel de código se cumple lo que se está en los alineamientos de la fase 2):** paquetes 3 y 4 primero. Con ellos se muestra el recorrido completo: una empresa publica una vacante, un estudiante verificado la ve y se postula, la empresa cambia el estado, y otra empresa recibe 404 si intenta ver esa postulación.

## Cómo se hace una HU (patrón que ya existe)

Copiar la estructura de `Features/Auth` y `AuthController`. No hace falta tocar `Program.cs`: los handlers y validadores se registran solos.

```
backend/src/UPractice.Application/Features/Vacantes/
  VacantesDtos.cs          ← records con los nombres de docs/contratos/api-types.ts
  CrearVacanteValidator.cs ← AbstractValidator<CrearVacanteRequest>, mensajes de docs/mensajes-validacion.md
  CrearVacanteHandler.cs   ← recibe IAppDbContext e ICurrentUser; lanza excepciones con CodigosError
backend/src/UPractice.Api/Controllers/
  EmpresaVacantesController.cs ← [Route("api/v1/empresa/vacantes")] [Authorize(Roles = "AdminEmpresa,Reclutador")]
```

&nbsp;

| Regla | Por qué |
| :---- | :---- |
| El `EmpresaId` y el `UniversidadId` salen de `ICurrentUser`, nunca del body ni de la URL | Lineamiento de la Fase 2 y aislamiento entre empresas |
| No escribir `Where(x => x.EmpresaId == …)` a mano; el filtro global ya lo hace | Si no existe para el usuario, se lanza `NoEncontradoException` → 404 |
| No usar `IgnoreQueryFilters()` en los handlers | Solo se permite en login, seed y pruebas; se revisa en el PR |
| Errores de negocio con `ReglaNegocioException(CodigosError.X, "…")` o `ConflictoException` | El middleware arma el 422 o 409 con el `code` que espera el frontend |
| Toda ruta lleva `[Authorize(Roles = "…")]` con los roles de la HU | Hoy ningún endpoint de negocio filtra por rol todavía |
| No crear migraciones salvo que cambie el ERD; si hace falta, avisar al equipo antes | Las 19 tablas ya existen; dos migraciones en paralelo chocan |
| Al menos una prueba por HU con `ApiFixture` (ver `AislamientoTests.cs`) | Definition of Done: prueba de aislamiento si toca datos de empresa |

## Flujo de trabajo con Git

1. Tomar el issue en el tablero, asignárselo y moverlo a *In progress*.

2. Partir siempre de `main` actualizado:

```
git switch main
git pull
git switch -c feat/HU-E03-crear-vacante
```

3. Compilar y correr las pruebas antes del PR (ver la siguiente sección).

4. Abrir el PR con `Closes #14` en la descripción (en inglés, una línea por issue) y pedir revisión a un compañero.

5. Fusionar con *Squash and merge*. Para la siguiente HU, volver al paso 2: nunca seguir trabajando en una rama ya fusionada.

## Preparar la máquina y probar

1. Requisitos: Git, Docker Desktop abierto y .NET SDK 10.0.401 o mayor (`dotnet --list-sdks`).

2. PostgreSQL local (una sola vez):

```
docker run -d --name upractice-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=upractice -p 5432:5432 -v upractice-pgdata:/var/lib/postgresql/data postgres:17-alpine
```

3. Contraseñas propias para el seed, desde `backend` (quedan en su PC, no en el repo):

```
dotnet user-secrets set "Seed:SuperAdminPassword" "<una>" --project src/UPractice.Api
dotnet user-secrets set "Seed:DemoPassword" "<otra>" --project src/UPractice.Api
```

4. Correr la API con `dotnet run --project src/UPractice.Api` y abrir `http://localhost:5294/swagger`. Iniciar sesión con `POST /api/v1/auth/login` y pegar el token en *Authorize*.

5. Pruebas: `dotnet test tests/UPractice.Tests` (necesita Docker abierto).

**Si Windows bloquea la API ("An application control policy has blocked this file")**

Es Smart App Control de Windows 11\. No hace falta apagarlo: el README tiene los comandos para correr la API y las pruebas dentro de Docker.

Cuentas de demo: `rrhh@digitales.example.com` (reclutador empresa A), `rrhh@finanzas.example.com` (reclutador empresa B), `ana.lopez@unah.hn` (estudiante verificada), `coordinacion@unah.hn` (coordinador). Todas usan la contraseña que cada uno puso en `Seed:DemoPassword`.

## Antes de pedir revisión

- Cumple los criterios de aceptación del issue  
- Rutas, DTO y códigos de error iguales al contrato (`docs/contratos/api-types.ts`)  
- Validador con los mensajes exactos de `docs/mensajes-validacion.md`  
- `[Authorize(Roles = …)]` en cada ruta  
- Prueba de aislamiento si toca datos de una empresa  
- Compila sin advertencias y todas las pruebas pasan  
- Se ve y se puede probar en Swagger  
- Sin secretos en el código ni en `appsettings`

## Fuera del alcance de la Fase 2

Frontend (Fase 3), Dockerfile y `docker-compose.yml` (Fase 4), despliegue y CI (Fase 5). Si alguien queda libre, el siguiente paso con más valor es T-05 (frontend base), que no depende del backend.

Repositorio [DeibiXD/UPractice](https://github.com/DeibiXD/UPractice) · Tablero [Sistemas Expertos UPractice](https://github.com/users/DeibiXD/projects/3) · Presentación final: 30 de noviembre al 4 de diciembre.