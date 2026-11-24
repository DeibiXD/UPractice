# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Restaurar antes de copiar el código permite reutilizar esta capa.
COPY global.json ./
COPY src/UPractice.Domain/UPractice.Domain.csproj src/UPractice.Domain/
COPY src/UPractice.Application/UPractice.Application.csproj src/UPractice.Application/
COPY src/UPractice.Infrastructure/UPractice.Infrastructure.csproj src/UPractice.Infrastructure/
COPY src/UPractice.Api/UPractice.Api.csproj src/UPractice.Api/
RUN dotnet restore src/UPractice.Api/UPractice.Api.csproj

COPY src/ src/
RUN dotnet publish src/UPractice.Api/UPractice.Api.csproj \
    --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
RUN apk add --no-cache icu-libs icu-data-full tzdata \
    && mkdir -p /app/storage \
    && chown "$APP_UID:$APP_UID" /app/storage

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_HTTP_PORTS=8080

WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "UPractice.Api.dll"]
