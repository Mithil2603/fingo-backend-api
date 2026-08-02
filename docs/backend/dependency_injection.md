# Dependency Injection Infrastructure Specification

## Purpose
This document defines the service registration patterns, dependency lifetimes, extension method organization, and composition root structure in Fingo.

## Scope
Applies to service registrations in `Program.cs`, infrastructure setup extensions, feature handler registration, and lifespan rules.

## Contents

### Principles of Composition Root Cleanliness
`Program.cs` acts strictly as the **Composition Root**. It must remain short, declarative, and free of inline configuration blocks or multi-line lambda expressions.

All configuration logic is divided into specialized static extension classes inside `Infrastructure/`:
- `DatabaseSetup.cs` -> Registers `IDbConnectionFactory`, DbContext.
- `AuthenticationSetup.cs` -> Registers JWT Bearer options, `IJwtProvider`, `IPasswordHasherService`.
- `ValidationSetup.cs` -> Registers FluentValidation assembly scans & filters.
- `FeatureServicesSetup.cs` -> Registers Vertical Slice Handlers.

```
+-----------------------------------------------------------------------------------+
|                            PROGRAM.CS COMPOSITION ROOT                            |
|                                                                                   |
|  builder.Services                                                                 |
|     .AddDatabaseInfrastructure(config)                                            |
|     .AddJwtAuthentication(config)                                                 |
|     .AddValidationInfrastructure()                                                |
|     .AddFeatureHandlers();                                                        |
|                                                                                   |
|  var app = builder.Build();                                                       |
|  app.UseMiddleware<ExceptionHandlingMiddleware>();                                |
|  app.MapControllers();                                                            |
+-----------------------------------------------------------------------------------+
```

### Lifespan Matrix & Usage Rules

| Lifespan | Usage Rule | Examples |
| :--- | :--- | :--- |
| **Transient** | Stateless utility components with no state holding requirements. | Guid generators, date time providers. |
| **Scoped** | Per-HTTP-request lifecycle. Must be used for all DB handlers, transactions, and HTTP contextual instances. | `IDbConnectionFactory`, `CreateTransactionHandler`, `ValidationFilter`. |
| **Singleton** | Application-wide shared instances. Must be stateless or thread-safe. | `IJwtProvider`, `IPasswordHasherService`, Application Configuration. |

## Best Practices
- Never inject a Scoped service into a Singleton service (Captive Dependency Anti-Pattern).
- Auto-scan feature handlers or use domain extension methods to avoid bloated registration files.
- Prefer Primary Constructors in C# 12 for clean constructor injection into handlers and endpoints.

## Concrete Examples

### 1. Auto-Scan Extension Method for Vertical Slice Handlers

```csharp
// File: Infrastructure/DependencyInjection/FeatureServicesSetup.cs
using System.Reflection;

namespace Fingo.BackendApi.Infrastructure.DependencyInjection;

public static class FeatureServicesSetup
{
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Scan all classes ending in "Handler" and register them as Scoped
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Handler"));

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
        }

        return services;
    }
}
```

### 2. Clean `Program.cs` Composition Root

```csharp
// File: Program.cs
using Fingo.BackendApi.Infrastructure.Authentication;
using Fingo.BackendApi.Infrastructure.Database;
using Fingo.BackendApi.Infrastructure.DependencyInjection;
using Fingo.BackendApi.Infrastructure.ErrorHandling;
using Fingo.BackendApi.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

// Register Infrastructure & Feature Services via Clean Extension Methods
builder.Services.AddDatabaseInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddValidationInfrastructure();
builder.Services.AddFeatureHandlers();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 3. C# 12 Primary Constructor Injection in Handlers

```csharp
// Example using C# 12 Primary Constructor for DI
namespace Fingo.BackendApi.Features.Categories.GetCategories;

using Fingo.BackendApi.Infrastructure.Database;

public class GetCategoriesHandler(IDbConnectionFactory connectionFactory, ILogger<GetCategoriesHandler> logger)
{
    public async Task<IEnumerable<CategoryDto>> HandleAsync(Guid userId)
    {
        logger.LogInformation("Retrieving categories for user {UserId}", userId);
        using var connection = connectionFactory.CreateConnection();
        // ... execute query
        return [];
    }
}

public record CategoryDto(Guid Id, string Name, string Type);
```

## References
- Dependency Injection in ASP.NET Core Official Guidelines
- Avoid Captive Dependencies in .NET

## Notes
- Service lifetimes must be strictly respected to prevent connection leaks and concurrency memory corruption.
