Lineamientos del Proyecto: Plataforma Web SaaS Multi-empresa

Diseñar, desarrollar, contenerizar y desplegar una plataforma web transaccional bajo el modelo SaaS (Software as a Service) Multi-empresa, cuya temática de negocio será definida por cada equipo. El sistema debe respetar Clean Architecture, Vertical Slicing, y ser desplegado en un ecosistema Cloud de costo cero.

🚀 Fase 1: Ideación, Análisis y Gobierno Ágil (15%)

El estudiante toma propiedad del producto definiendo un sistema B2B (Business-to-Business).

Definición del SaaS: Pitch de 1 página definiendo la plataforma (ej. SaaS para Gestión de Barberías, SaaS de Inventario para Ferreterías, SaaS para Clínicas Dentales).

Diseño Multi-empresa (Multi-tenant): Modelado del Diagrama Entidad-Relación (ERD) en 3ra Forma Normal (3FN). Todas las tablas transaccionales principales deben incluir su columna EmpresaId como llave foránea.

Tablero Kanban: Uso de GitHub Projects con Historias de Usuario claras.

Contratos de Interfaz: Definición del diseño de la API (Endpoints, DTOs de entrada y salida).

🛠️ Fase 2: Construcción del Backend Transaccional (.NET + PostgreSQL) (25%)

El motor del sistema, riguroso, multi-cliente y protegido.

Clean Architecture: Implementación estricta de capas (Domain, Application, Infrastructure, API).

Persistencia y Aislamiento de Datos: Entity Framework Core (Code-First). Implementación de Global Query Filters (HasQueryFilter) para garantizar que las consultas filtren automáticamente por el EmpresaId del usuario actual.

Seguridad IAM (JWT Claims): Autenticación con Tokens JWT. El EmpresaId debe viajar encriptado como un Claim dentro del token para que el Backend sepa a qué empresa pertenece el usuario que hace la petición.

Validación Semántica: Artefactos de validación con FluentValidation propagando los errores hacia la capa API mediante un Middleware global.

💻 Fase 3: Frontend Web Reactivo (React + TypeScript) (25%)

La interfaz modular y resiliente para los usuarios finales de cada empresa.

Arquitectura Vertical Slicing: Agrupación por funcionalidades (ej. features/auth, features/clientes, features/facturacion).

Rutas Protegidas: Guardias de navegación con react-router-dom para bloquear usuarios sin JWT.

Validación Sintáctica en Vivo: Zod + React Hook Form (mode: 'onChange'), en perfecta simetría con FluentValidation del Backend.

Resiliencia HTTP: Extractor universal de errores (el programado en la Clase 16) e Interceptores de Axios para adjuntar el JWT (Bearer) en cada petición.

🐳 Fase 4: Contenerización (Dockerización Local) (15%)

Estandarizando el entorno de desarrollo para erradicar el "en mi máquina sí funciona".

Dockerfiles: Dockerfile Multi-stage para la API en .NET (SDK para compilar, Runtime ligero de Alpine para ejecutar).

Docker Compose Local: Creación de un docker-compose.yml que levante la API (.NET) conectada a un contenedor efímero de PostgreSQL para desarrollo local unificado en el equipo.

☁️ Fase 5: Despliegue Cloud Automatizado (El Zero-Cost Stack) (20%)

Despliegue a producción utilizando infraestructura moderna, Serverless y 100% gratuita para educación.

Base de Datos (Neon.tech o Supabase): Plataforma de PostgreSQL en la nube, gratuita y sin tarjeta de crédito.

Backend Web Service (Render.com o Fly.io): Plataforma PaaS conectada a GitHub. Compila el Dockerfile de .NET y expone la API en un dominio HTTPS automático.

Frontend SPA (Vercel o Netlify): Despliegue del cliente React/Vite en segundos con un CDN global, conectado a la rama main de GitHub.

Integración Continua Básica: Cada git push a la rama principal actualizará automáticamente todo el ecosistema SaaS en producción.