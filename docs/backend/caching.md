# Caching Architecture & Invalidation Strategy Specification

## Purpose
This document defines the caching strategy, cache-aside pattern execution, cache key naming standards, Time-to-Live (TTL) policies, and invalidation triggers for the Fingo ASP.NET Core 8 Web API.

## Scope
Applies to all high-frequency read endpoints, aggregated financial metrics (Dashboard net worth, monthly cash flow totals, category summaries), and reference data queries.

## Contents

### Caching Philosophy
Fingo utilizes a **Cache-Aside Pattern** (Lazy Loading) to minimize PostgreSQL query overhead for read-heavy financial summaries without compromising real-time data accuracy.

```
+-----------------------------------------------------------------------+
|                         CACHE-ASIDE PATTERN                           |
|                                                                       |
|  1. Check Cache (IMemoryCache / IDistributedCache)                    |
|     +--> [HIT]  --> Return cached DTO directly                       |
|     +--> [MISS] --> Query PostgreSQL via Dapper                       |
|                     --> Populate Cache with TTL                       |
|                     --> Return fresh DTO                              |
+-----------------------------------------------------------------------+
```

### Cache Providers
1. **Local Development / Single-Node:** `IMemoryCache` (In-Memory).
2. **Production / Distributed:** Redis via `IDistributedCache` (`Microsoft.Extensions.Caching.StackExchangeRedis`).

### Cache Key Naming Standard
Cache keys must follow a strict, collision-free hierarchical format:

`fingo:{domain}:{userId}:{entityOrQuery}`

#### Examples:
- User Dashboard Summary: `fingo:dashboard:usr_9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d:summary`
- Category List: `fingo:categories:usr_9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d:all`
- Account List: `fingo:accounts:usr_9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d:all`

### Time-To-Live (TTL) Policy

| Data Category | Volatility | Absolute Expiration | Sliding Expiration |
| :--- | :--- | :--- | :--- |
| **Reference Categories** | Low | 60 minutes | 15 minutes |
| **Account Metadata** | Medium | 15 minutes | 5 minutes |
| **Dashboard Metrics** | High | 5 minutes | 2 minutes |
| **Financial Reports** | Very High | 2 minutes | 1 minute |

### Cache Invalidation Triggers
Data mutations **MUST** trigger immediate cache eviction for the affected user scope:
- **Transaction Created / Updated / Deleted** -> Evict `fingo:dashboard:{userId}:*`, `fingo:reports:{userId}:*`, `fingo:accounts:{userId}:*`.
- **Account Created / Updated / Deleted** -> Evict `fingo:accounts:{userId}:*`, `fingo:dashboard:{userId}:*`.
- **Category Created / Updated / Deleted** -> Evict `fingo:categories:{userId}:*`.

## Best Practices
- Never cache raw database entities; cache strongly typed Response DTOs.
- Always scope cache keys by `userId` to prevent data leakage between tenants.
- Use `System.Text.Json` for Redis byte array serialization.

## Concrete Code Example

```csharp
// File: Infrastructure/Caching/CacheService.cs
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Fingo.BackendApi.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken);
        if (bytes == null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration
        };
        await _cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }
}
```

## References
- [ASP.NET Core Caching Overview](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/overview)
- [Project Decisions ADR](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/architecture/project_decisions.md)
