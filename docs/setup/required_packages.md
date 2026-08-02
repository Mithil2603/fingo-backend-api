# Approved Nuget Packages Inventory & Versioning Specification

## Purpose
This document catalogs approved NuGet packages, target framework requirements, version rules, and banned third-party dependencies for the Fingo backend.

## Scope
Applies to all project files (`.csproj`), package references, and third-party library additions.

## Contents

### Target Framework & Language Standard
- **Target Framework:** `net8.0` (ASP.NET Core 8 Web API)
- **Language Standard:** C# 12 (`<LangVersion>12.0</LangVersion>`)
- **Nullable Context:** Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings:** Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)

### Approved Package Inventory

| Package Name | Approved Version | Purpose | Permitted Scope |
| :--- | :--- | :--- | :--- |
| `Dapper` | `2.1.79+` | High-performance micro-ORM for SQL query execution | Runtime DB access in Handlers |
| `Npgsql` / `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3+` / `8.0.+` | PostgreSQL database provider & EF Core migrations | DB connections & EF schema migrations |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `8.0.+` | JWT token authentication middleware | Security infrastructure |
| `FluentValidation.AspNetCore` | `11.0.+` | Automated request payload validation | Request DTO validation |
| `Swashbuckle.AspNetCore` | `6.6.2+` | Swagger OpenAPI specification generation | API documentation |

### Explicitly Banned Packages List

| Banned Package Name | Rationale for Ban | Mandatory Alternative |
| :--- | :--- | :--- |
| **`MediatR`** | Obfuscated control flow, reflection overhead, indirect debugging. | Strongly typed feature Handlers registered in DI. |
| **`AutoMapper`** | Unpredictable mapping behavior, runtime mapping exceptions, slow reflection. | Explicit constructor or record positional mappings. |
| **`Newtonsoft.Json`** | Performance overhead, duplicate JSON engine. | Native `System.Text.Json`. |
| **`EntityFrameworkCore` (CRUD)** | Heavy memory footprint, complex tracking overhead, generic query generation. | Micro-ORM **Dapper** for runtime queries. |

## Best Practices
- Keep dependencies lean. Propose new package additions via Architectural Decision Review.
- Pin minor package versions to avoid breaking changes across build environments.

## Concrete Examples

### Project File (`fingo-backend-api.csproj`) Specification

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Fingo.BackendApi</RootNamespace>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core Data & DB Packages -->
    <PackageReference Include="Dapper" Version="2.1.79" />
    <PackageReference Include="Npgsql" Version="10.0.3" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />

    <!-- Security & Infrastructure Packages -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.10" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

</Project>
```

## References
- NuGet Package Security Guidelines
- .NET 8 Performance Improvements in Standard Libraries

## Notes
- Installing MediatR or AutoMapper will break automated pull request checks.
