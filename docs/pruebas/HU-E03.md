# Probar la creación y publicación de vacantes

Esta guía cubre el primer bloque backend de HU-E03: `POST /api/v1/empresa/vacantes`. Usa el PostgreSQL local y la API en Development. Los catálogos GET, el panel de vacantes y el frontend se integran en sus entregas correspondientes.

## Preparación

Mantén Docker Desktop y `upractice-pg` funcionando. Si tienes la API anterior abierta, detenla con Ctrl+C para cargar el código nuevo. Desde PowerShell:

```powershell
cd C:\Proyectos\UPractice\backend
dotnet run --project .\src\UPractice.Api --launch-profile http
```

La API aplica la migración existente y el seed según su configuración. Las contraseñas del seed se configuran con user-secrets como explica el README; esta guía no incluye sus valores. Abre `http://localhost:5294/swagger` e inicia sesión mediante `POST /api/v1/auth/login` con `rrhh@digitales.example.com` y tu `Seed:DemoPassword`. Pega solo `accessToken` en Authorize.

## Obtener una carrera permitida del seed

Mientras se integran los GET de catálogos, puedes consultar IDs en la BD local. Abre otra terminal; este comando solo lee datos:

```powershell
@'
SELECT c."Id" AS "carreraId", c."Nombre", u."Siglas"
FROM "Carreras" c
JOIN "Facultades" f ON f."Id" = c."FacultadId"
JOIN "Universidades" u ON u."Id" = f."UniversidadId"
JOIN "HabilitacionesEmpresa" h ON h."UniversidadId" = u."Id"
JOIN "Usuarios" a ON a."EmpresaId" = h."EmpresaId"
WHERE a."Correo" = 'rrhh@digitales.example.com'
  AND h."Estado" = 'Aprobada' AND c."Activa" AND u."Activa"
ORDER BY u."Siglas", c."Nombre";
'@ | docker exec -i upractice-pg psql -U postgres -d upractice
```

Copia un `carreraId` real de esa salida. Los IDs se generan al cargar el seed; no uses un GUID de ejemplo como si perteneciera a tu BD.

## Crear un borrador

En Swagger, abre `POST /api/v1/empresa/vacantes` y Try it out. Sustituye el marcador de carrera por el ID obtenido y ajusta la fecha para que sea posterior a hoy en Honduras:

```json
{
  "titulo": "Practicante de desarrollo backend",
  "descripcion": "Apoyo al equipo de desarrollo en APIs y pruebas automatizadas con .NET y PostgreSQL.",
  "modalidad": "Hibrida",
  "ubicacion": "Tegucigalpa",
  "cupos": 2,
  "remunerada": true,
  "indiceMinimo": 75,
  "fechaCierre": "2026-12-15",
  "carrerasIds": ["<carreraId real>"],
  "habilidades": [],
  "publicar": false
}
```

Resultado esperado: 201, estado Borrador, fechaPublicacion null, creador y carreras correctos, totalPostulaciones cero. `Location` contiene `/api/v1/empresa/vacantes/{id}`; el GET de ese recurso pertenece a HU-E04 y no se implementa en este primer bloque.

## Publicar y agregar habilidades

Repite el POST con `publicar=true`. La empresa debe tener habilitación Aprobada para las universidades de todas las carreras elegidas. Resultado esperado: 201, estado Publicada y fechaPublicacion UTC.

Para agregar habilidades, consulta sus IDs:

```powershell
@'
SELECT "Id", "Nombre" FROM "Habilidades" ORDER BY "Nombre" LIMIT 10;
'@ | docker exec -i upractice-pg psql -U postgres -d upractice
```

Usa un ID real y envía explícitamente si se requiere:

```json
"habilidades": [
  { "habilidadId": "<habilidadId real>", "esRequerida": true }
]
```

`modalidad`, `remunerada`, `publicar` y `esRequerida` son campos explícitos del contrato. El índice mínimo es opcional (`null`) y se devuelve con la precisión realmente persistida en PostgreSQL.

## Comprobar errores

- Un título de un carácter devuelve 400 con `errors.titulo`.
- Cupos cero devuelve 400 con `errors.cupos`.
- Fecha de cierre hoy o pasada devuelve 400 con `errors.fechaCierre`, calculada en Honduras.
- IDs de carrera o habilidad repetidos devuelven 400 en el campo correspondiente.
- Publicar sin aprobación devuelve 422 `EMPRESA_NO_HABILITADA`.
- Una carrera de universidad no habilitada devuelve 422 `CARRERA_NO_PERMITIDA`, también en borradores.
- Sin token se obtiene 401; otro rol o una contraseña temporal pendiente produce 403.

La empresa y el autor siempre se obtienen de la identidad autenticada. La suite prepara empresas y aprobaciones propias para los casos negativos; no hace falta alterar las habilitaciones del seed local para comprobarlos.

Para documentar tus comprobaciones en Swagger y DBeaver, completa el [registro de pruebas manuales](HU-E03-resultados-manuales.md) y adjunta las capturas de los casos ejecutados.

## Pruebas automatizadas

Con Docker funcionando, desde `backend`:

```powershell
dotnet build UPractice.slnx
dotnet test .\tests\UPractice.Tests\UPractice.Tests.csproj
```

Las integraciones usan una BD temporal propia y limpian sus escenarios. Incluyen creación, publicación, permisos, identidad/tenant, referencias, errores, contrato OpenAPI y comparación entre respuesta y datos persistidos. Las pruebas unitarias controlan el reloj para comprobar límites de fecha sin depender de la hora real.

Verificación del 16 de noviembre de 2026, después de integrar `main` (`46dded5`) en la rama de HU-E03: `dotnet build UPractice.slnx` terminó correctamente y la suite completa ejecutó **102 pruebas correctas, cero fallidas y cero omitidas**. El registro de la prueba `Universidad_con_usuarios_no_puede_eliminarse_fisicamente` incluye una excepción de PostgreSQL esperada: comprueba la restricción de borrado y no representa un fallo de la suite.

Esta entrega de backend no acredita por sí sola el cierre de la HU de punta a punta: siguen la integración de catálogos, frontend/Zod y las condiciones de revisión, fusión y despliegue.
