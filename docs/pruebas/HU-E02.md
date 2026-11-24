# Probar la gestión de usuarios de empresa (HU-E02)

Esta entrega backend implementa los tres endpoints de gestión y reutiliza
`PUT /api/v1/auth/password` para el cambio obligatorio de contraseña (D-17).
Fuentes: HU-E02 y §2.2 del [diseño](../FASES/UPractice-Fase1.md),
[contratos vigentes](../contratos/api-types.ts) y
[mensajes compartidos](../mensajes-validacion.md).

## Preparación

Mantén Docker Desktop y tu PostgreSQL local funcionando. Reinicia la API para
cargar estos cambios. Desde PowerShell:

```powershell
cd C:\Proyectos\UPractice\backend
dotnet run --project .\src\UPractice.Api --launch-profile http
```

Abre `http://localhost:5294/swagger`. Con el seed de desarrollo, inicia sesión
en `POST /api/v1/auth/login` como `admin@digitales.example.com`, usando tu
`Seed:DemoPassword` o la contraseña que hayas cambiado. Pega el `accessToken`
en **Authorize**. Comprueba el rol `AdminEmpresa` en `GET /api/v1/auth/me` y
guarda tu propio `id` para la prueba de autodesactivación.

Si esa cuenta tiene contraseña temporal pendiente, completa primero
`PUT /api/v1/auth/password` y usa el nuevo token de su respuesta.

## Listar y crear cuentas

`GET /api/v1/empresa/usuarios` devuelve un arreglo, sin paginación, con las
cuentas activas e inactivas de tu empresa, incluida la propia. Cada elemento
contiene únicamente `id`, `nombreCompleto`, `correo`, `rol` y `activo`.

En `POST /api/v1/empresa/usuarios`, envía:

```json
{
  "nombre1": "María",
  "nombre2": null,
  "apellido1": "López",
  "apellido2": "Castro",
  "correo": "reclutador.hu-e02@example.com",
  "rol": "Reclutador"
}
```

Espera **201** con `usuarioId`, `correo` y `passwordTemporal`.
`Location` apunta a `/api/v1/empresa/usuarios`, donde puedes verificar la cuenta
con GET. La contraseña temporal tiene 20 caracteres y solo se entrega en ese
POST; el listado nunca permite recuperarla. Para probar una segunda creación,
elige otro correo y usa `rol: "AdminEmpresa"`.

No se envían `empresaId`, `universidadId`, `password`, `activo` ni
`debeCambiarPassword` al crear. La API asigna la empresa del usuario autenticado
y crea una cuenta activa con contraseña temporal. Nombres y correo se recortan;
el correo se guarda en minúsculas y los nombres opcionales vacíos como `null`.
No se exige habilitación aprobada para administrar al equipo.

## Comprobar el primer acceso

1. Inicia sesión con el correo nuevo y su `passwordTemporal`.
2. Autoriza con ese token: `/auth/me` debe indicar `debeCambiarPassword: true`.
3. Si creaste un `AdminEmpresa`, su GET `/empresa/usuarios` debe responder
   **403 `CAMBIO_PASSWORD_REQUERIDO`** hasta cambiar la contraseña.
4. Ejecuta `PUT /api/v1/auth/password` con los campos `passwordActual`
   (la temporal), `passwordNuevo` y `confirmarPassword` (iguales entre sí).
   Elige una nueva de 8–64 caracteres, con mayúscula, minúscula y número.
5. Espera 200, `debeCambiarPassword: false` y usa el **nuevo token**.
   El administrador podrá listar usuarios. Un reclutador seguirá obteniendo
   403 en la gestión, debido a su rol.
6. Comprueba que la contraseña temporal ya no permite iniciar sesión y la
   nueva sí. No guardes contraseñas ni tokens en capturas o archivos del PR.

## Desactivar y reactivar

Vuelve a autorizar como el administrador original y ejecuta
`PATCH /api/v1/empresa/usuarios/{usuarioId}/estado` sobre la cuenta creada:

```json
{ "activo": false }
```

Espera **204**, verifica `activo: false` en el listado e intenta iniciar sesión
como esa cuenta: debe devolver **401**. Repite el PATCH con `activo: true`:
debe devolver 204 y permitir el login con su contraseña actual.
La reactivación conserva la contraseña y su marca de cambio pendiente;
no genera una nueva. Enviar el estado que ya tiene también devuelve 204.

## Casos negativos y aislamiento

| Caso | Resultado esperado |
|---|---|
| Sin token en cualquiera de las tres rutas | 401 |
| Token de Reclutador, Estudiante, Coordinador o SuperAdmin | 403 |
| Nombre vacío, de un carácter tras recortarlo o con números | 400 en `errors.nombre1` |
| Correo inválido o de más de 150 caracteres | 400 en `errors.correo` |
| Crear con rol conocido distinto de los dos permitidos | 400 en `errors.rol` |
| Omitir `rol`, enviar `{}` al PATCH o un tipo incorrecto | 400 de JSON/ModelState |
| Repetir un correo, incluso con mayúsculas/espacios o de otra empresa | 409 `CORREO_EN_USO` |
| Intentar desactivarte usando tu propio ID | 422 `TRANSICION_INVALIDA` |
| Activarte a ti mismo | 204 |
| Cambiar estado de un ID inexistente o de otra empresa | 404 |
| Administrador desactivado, trasladado o con rol revocado usando su token anterior | 401 en estas rutas |
| Empresa del administrador suspendida | 401 en estas rutas |

Para contrastar A/B, inicia sesión también como `admin@finanzas.example.com`
con su contraseña local. Obtén sus IDs con su propio GET. El GET de Digitales
no debe incluirlos, y su PATCH hacia esos IDs debe devolver 404 sin cambiarlos.
Comprueba con el administrador de Finanzas que su usuario conserva el estado.
La suite también verifica por HTTP el aislamiento de vacantes y postulaciones
mediante las rutas existentes de postulantes y detalle de postulación.

## Pruebas automatizadas y registro manual

Con Docker funcionando, desde `backend`:

```powershell
dotnet build UPractice.slnx
dotnet test .\tests\UPractice.Tests\UPractice.Tests.csproj
```

Las integraciones usan PostgreSQL temporal y escenarios propios. Cubren
contratos HTTP/OpenAPI, permisos, aislamiento, duplicados, persistencia,
normalización, el ciclo de contraseña temporal y las bajas/reactivaciones.
Las pruebas no modifican tu base de desarrollo.

Verificación automatizada del **22 de noviembre de 2026** en la rama
`codex/hu-e02-gestion-reclutadores`, basada en `757cdaf`: compilación de la
solución correcta, sin advertencias ni errores; **170 pruebas correctas,
cero fallidas y cero omitidas**. La base tenía 113 pruebas; se añadieron
57 casos de HU-E02 (38 HTTP/integración y 19 unitarios).
Este resultado no sustituye las comprobaciones manuales de la tabla siguiente.

Para registrar tus pruebas manuales, copia esta tabla y completa solo lo
ejecutado. Incluye capturas con las credenciales y tokens ocultos.

| Fecha y commit probado | Caso | HTTP esperado / obtenido | Resultado | Evidencia sin secretos |
|---|---|---|---|---|
| Pendiente | Creación y primer acceso | 201 / pendiente | Pendiente | Pendiente |
| Pendiente | Desactivar/reactivar | 204 y login 401 / pendiente | Pendiente | Pendiente |
| Pendiente | Autodesactivación | 422 / pendiente | Pendiente | Pendiente |
| Pendiente | Rol Reclutador | 403 / pendiente | Pendiente | Pendiente |
| Pendiente | Aislamiento A/B | 404 / pendiente | Pendiente | Pendiente |

El cierre de la HU completa conserva pendientes el formulario y su esquema
Zod simétrico, además de aprobación del PR, fusión y despliegue.
