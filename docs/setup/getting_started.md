# Developer Onboarding & Getting Started Guide

## Purpose
This document provides a step-by-step walkthrough for developers to clone, configure, build, run, and test the Fingo backend API on a local development machine.

## Scope
Applies to local environment setup, SDK prerequisites, database bootstrapping, EF Core migration execution, and initial API testing via Swagger UI.

## Contents

### Technical Prerequisites
Before setting up the repository, ensure your environment has the following installed:
1. **.NET 8.0 SDK** (v8.0.100 or higher) -> Run `dotnet --version` to verify.
2. **PostgreSQL 16+** server running locally or via Docker.
3. **IDE / Editor:** Visual Studio 2022 (v17.8+), VS Code with C# Dev Kit extension, or JetBrains Rider.
4. **Git CLI** for repository cloning.

### Step-by-Step Developer Walkthrough

#### Step 1: Clone Repository
```bash
git clone https://github.com/your-org/fingo-backend-api.git
cd fingo-backend-api
```

#### Step 2: Configure PostgreSQL Database
Ensure a local PostgreSQL server is running. Create a new database named `fingo_db`:
```sql
CREATE DATABASE fingo_db;
```

#### Step 3: Configure `appsettings.Development.json`
Update `appsettings.Development.json` with your local PostgreSQL credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fingo_db;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "SecretKey": "SuperSecretKeyForLocalDevelopmentEnvironmentOnlyMustBeAtLeast32BytesLong!",
    "Issuer": "FingoApiLocal",
    "Audience": "FingoClientLocal",
    "ExpirationMinutes": "120"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### Step 4: Apply Database Migrations (EF Core CLI)
Run EF Core CLI commands to apply initial table migrations to PostgreSQL:
```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```

#### Step 5: Build and Run Application
```bash
dotnet build
dotnet run
```

#### Step 6: Verify API in Swagger UI
Open your browser and navigate to:
`https://localhost:7050/swagger` (or `http://localhost:5050/swagger`)

Verify that OpenAPI interactive documentation loads and endpoints are accessible.

## Best Practices
- Never commit real secrets or production passwords into `appsettings.json`. Use environment variables or User Secrets (`dotnet user-secrets set`).
- Always run `dotnet ef database update` after pulling new changes from Git.

## Concrete Examples

### CLI Command Summary Cheat-Sheet

```powershell
# Restore dependencies
dotnet restore

# Build solution in Release mode
dotnet build --configuration Release

# Run backend project locally
dotnet run --project fingo-backend-api.csproj

# Create a new EF Core database schema migration
dotnet ef migrations add AddBudgetsTable

# Update target database with latest migrations
dotnet ef database update
```

## References
- .NET 8 CLI Tools Reference
- EF Core Migrations Documentation
- PostgreSQL Local Installation Walkthrough

## Notes
- Ensure port 5432 (PostgreSQL) and 7050 (HTTPS Web API) are available locally.
