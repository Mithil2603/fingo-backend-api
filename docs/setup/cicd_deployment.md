# CI/CD Pipeline & Production Deployment Specification

## Purpose
This document defines the automated Continuous Integration and Continuous Deployment (CI/CD) workflow, Docker containerization build specs, environment progression, and database migration deployment rules for `fingo-backend-api`.

## Scope
Applies to build automation scripts, GitHub Actions workflows, Docker image packaging, database migration execution, and production hosting configurations.

## Contents

### Containerization Strategy (`Dockerfile`)
Fingo uses multi-stage Docker builds based on official Microsoft .NET 8 Alpine runtime images to produce minimal, secure, non-root execution containers.

```dockerfile
# File: fingo-backend-api/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project definition and restore dependencies
COPY ["fingo-backend-api.csproj", "./"]
RUN dotnet restore "fingo-backend-api.csproj"

# Copy full source and publish release build
COPY . .
RUN dotnet publish "fingo-backend-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Run as unprivileged non-root user
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "fingo-backend-api.dll"]
```

### Database Migration Deployment Strategy
1. **Local / Development:** Automatic application of migrations using `dotnet ef database update` via developer CLI.
2. **Staging / Production:**
   - **NO AUTOMATIC MIGRATION ON APP STARTUP:** Executing `DbContext.Database.Migrate()` inside Web API startup is strictly banned in multi-node production deployments to avoid table lock deadlocks.
   - **Idempotent Migration Script Generation:** CI pipeline generates SQL migration scripts during build:
     `dotnet ef migrations script --idempotent --output /artifacts/migration.sql`
   - **Pipeline Migration Step:** The CI/CD deployment runner executes `migration.sql` against PostgreSQL *prior* to rolling out updated API container instances.

### GitHub Actions CI/CD Pipeline

```yaml
# File: .github/workflows/backend-cicd.yml
name: Fingo Backend CI/CD Pipeline

on:
  push:
    branches: [ main, staging ]
  pull_request:
    branches: [ main, staging ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore dependencies
        run: dotnet restore fingo-backend-api/fingo-backend-api.csproj
      - name: Build
        run: dotnet build fingo-backend-api/fingo-backend-api.csproj --no-restore -c Release
      - name: Run Unit & Integration Tests
        run: dotnet test fingo-backend-api/fingo-backend-api.csproj --no-build -c Release

  deploy-staging:
    needs: build-and-test
    if: github.ref == 'refs/heads/staging'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build Docker Image
        run: docker build -t fingo-backend:staging ./fingo-backend-api
```

## References
- [Official .NET Container Images](https://hub.docker.com/_/microsoft-dotnet-aspnet)
- [EF Core Idempotent Script Generation Guide](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli#generate-sql-scripts)
