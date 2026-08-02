# Environment Configuration & Docker Setup Specification

## Purpose
This document specifies configuration management, environment variable keys, Docker Compose infrastructure orchestration, and deployment settings for Fingo.

## Scope
Applies to local development, staging, and production application settings, environment overrides, and containerization.

## Contents

### Configuration Key Dictionary

| Configuration Path | Type | Default / Required Value | Purpose |
| :--- | :--- | :--- | :--- |
| `ConnectionStrings:DefaultConnection` | `string` | PostgreSQL connection string | Primary database host connection |
| `Jwt:SecretKey` | `string` | 256-bit secret string (Min 32 chars) | HMAC-SHA256 signature key for JWT |
| `Jwt:Issuer` | `string` | E.g. `Fingo.Api` | Token issuer identifier |
| `Jwt:Audience` | `string` | E.g. `Fingo.Client` | Token target audience identifier |
| `Jwt:ExpirationMinutes` | `integer` | E.g. `60` | Access token lifespan in minutes |
| `Logging:LogLevel:Default` | `string` | `Information` / `Warning` | Minimum logging threshold |

### Production Environment Variables Mapping
In production environments (e.g. Kubernetes, AWS ECS, Azure App Service), environment variables override settings in `appsettings.json`:
- `ConnectionStrings__DefaultConnection`
- `Jwt__SecretKey`
- `Jwt__Issuer`
- `Jwt__Audience`

```
+-----------------------------------------------------------------------------------+
|                        ENVIRONMENT CONFIGURATION OVERRIDE                         |
|                                                                                   |
|  appsettings.json <--- appsettings.Development.json <--- OS Environment Variables|
|     (Base Config)           (Local Overrides)            (High Priority Overrides)|
+-----------------------------------------------------------------------------------+
```

## Best Practices
- Never check production DB passwords or JWT secret keys into Git repositories.
- Use Docker Compose for spinning up PostgreSQL database containers locally.

## Concrete Examples

### 1. Complete `docker-compose.yml` for Local Development

```yaml
version: '3.8'

services:
  fingo-db:
    image: postgres:16-alpine
    container_name: fingo-postgres
    restart: always
    environment:
      POSTGRES_DB: fingo_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: SecretPassword123!
    ports:
      - "5432:5432"
    volumes:
      - postgres_fingo_data:/var/lib/postgresql/data

volumes:
  postgres_fingo_data:
```

### 2. Standard `appsettings.json` Base Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fingo_db;Username=postgres;Password=SecretPassword123!"
  },
  "Jwt": {
    "SecretKey": "ReplaceWithSuperLongAndSecretProductionKeyThatIsAtLeast32BytesInLength!",
    "Issuer": "FingoApi",
    "Audience": "FingoClients",
    "ExpirationMinutes": 60
  }
}
```

## References
- Configuration in ASP.NET Core Official Guide
- Docker Compose File Reference

## Notes
- Environment names are typically `Development`, `Staging`, and `Production`.
