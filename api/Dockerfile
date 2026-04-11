# syntax=docker/dockerfile:1

# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# Copy project files first so the restore layer is cached until .csproj files change
COPY src/MarathonTraining.Domain/MarathonTraining.Domain.csproj             src/MarathonTraining.Domain/
COPY src/MarathonTraining.Application/MarathonTraining.Application.csproj   src/MarathonTraining.Application/
COPY src/MarathonTraining.Infrastructure/MarathonTraining.Infrastructure.csproj src/MarathonTraining.Infrastructure/
COPY src/MarathonTraining.Api/MarathonTraining.Api.csproj                   src/MarathonTraining.Api/

RUN dotnet restore src/MarathonTraining.Api/MarathonTraining.Api.csproj

# Copy remaining source and publish
COPY src/ src/

RUN dotnet publish src/MarathonTraining.Api/MarathonTraining.Api.csproj \
      --no-restore \
      --configuration Release \
      --output /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as a non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "MarathonTraining.Api.dll"]
