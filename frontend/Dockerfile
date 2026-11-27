# syntax=docker/dockerfile:1

# ---------- Etapa 1: compilar con Node ----------
FROM node:22-alpine AS build
WORKDIR /app

# Primero solo las dependencias: Docker reutiliza esta capa si package*.json no cambia.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Vite incrusta la URL de la API en el build (no se puede cambiar en tiempo de ejecución).
ARG VITE_API_URL=http://localhost:8080/api/v1
ENV VITE_API_URL=${VITE_API_URL}
RUN npm run build

# ---------- Etapa 2: servir los archivos estáticos con nginx (sin root) ----------
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
