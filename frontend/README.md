# Frontend UPractice

React + TypeScript + Vite · MUI · React Router · React Hook Form + Zod · TanStack Query · Axios.

```bash
cd frontend
cp example.env .env.development     # VITE_API_URL (por defecto http://localhost:5294/api/v1)
npm install
npm run dev                          # http://localhost:5173  (la API debe estar corriendo)
```

| Comando | Qué hace |
|---|---|
| `npm run build` | Chequeo de tipos + build de producción |
| `npm run lint` | oxlint (incluye la regla de aislamiento entre features) |
| `npm test` | Pruebas (extractor de errores y esquemas Zod) |

## Estructura

```
src/
  app/        providers.tsx · router.tsx (solo compone las rutas de cada feature) · tema.ts
  shared/
    api/      http.ts (Axios + interceptores) · errores.ts (extractor universal) · types.ts (DTO de docs/contratos)
    auth/     AuthContext · useAuth · RutaProtegida · RutaPorRol · rutaInicio · sesion
    components/  Layout · Cargando · PaginaSinAcceso · PaginaNoEncontrada
    lib/      fechas.ts · validaciones.ts (mensajes y reglas Zod compartidos)
  features/<nombre>/
    pages/        pantallas (si hay varios roles: pages/estudiante/, pages/empresa/…)
    components/   piezas propias de la feature
    schemas/      esquemas Zod del formulario
    api.ts        llamadas con `http`
    routes.tsx    rutas de la feature (con su RutaPorRol)
    index.ts      lo único que otras partes pueden importar
```

Reglas (el linter las hace cumplir):
- Una feature **no** importa archivos internos de otra: solo `@/features/<nombre>` (su `index.ts`).
- `src/shared` **no** importa de `features`.

## Agregar una pantalla (checklist)

1. Crear `src/features/<nombre>/` con `pages/`, `schemas/`, `api.ts`, `routes.tsx`, `index.ts`.
2. En `routes.tsx` exportar `rutasPrivadas: RouteObject[]` (o `rutasPublicas`). Las privadas llevan su guardia de rol:
   `{ element: <RutaPorRol roles={['Coordinador']} />, children: [{ path: '/coordinacion/empresas', element: <Pagina /> }] }`
3. En `src/app/router.tsx`: importar las rutas y agregarlas con spread donde dice `FEATURES`.
4. Copiar los DTO que falten de `docs/contratos/api-types.ts` a `src/shared/api/types.ts` (el archivo es copia tal cual).
5. Formularios: `useForm({ resolver: zodResolver(esquema), mode: 'onChange' })`, mensajes de `docs/mensajes-validacion.md`
   (reutilice `shared/lib/validaciones.ts`) y, al fallar la API, `aplicarErrores(extraerError(e), setError)`.
6. Cuando la pantalla principal de un rol exista, cambiar su línea en `src/shared/auth/rutaInicio.ts`.

## Errores de la API

Todo error pasa por `extraerError(error)` → `{ mensaje, campos, code?, status?, traceId? }`.
Maneja: sin conexión o API dormida, 400 con `errors` por campo, 401, 403, 404, 409/422 con `code` y 5xx con `traceId`.
El interceptor redirige a `/login` en 401 y a `/cambiar-password` en 403 `CAMBIO_PASSWORD_REQUERIDO` (D-17).
