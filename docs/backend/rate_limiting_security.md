# Rate Limiting & API Security Specification

## Purpose
This document defines the rate limiting policy, brute-force mitigation on authentication endpoints, CORS rules, HTTP security headers, and sensitive PII data protection standards for Fingo.

## Scope
Applies to all HTTP request pipelines, middleware configurations, authentication endpoints, and logging sinks in `fingo-backend-api`.

## Contents

### Rate Limiting Policy
Fingo uses native ASP.NET Core Rate Limiting (`Microsoft.AspNetCore.RateLimiting`) configured with Sliding Window algorithms to protect database resources and prevent denial-of-service attacks.

#### Endpoint Rate Limit Tiers

| Tier Name | Target Routes | Permit Limit | Window | Queue Limit |
| :--- | :--- | :--- | :--- | :--- |
| **StrictAuth** | `/api/v1/auth/login`, `/api/v1/auth/register`, `/api/v1/auth/forgot-password` | 5 requests | 1 minute | 0 |
| **StandardApi** | `/api/v1/transactions/*`, `/api/v1/accounts/*`, `/api/v1/categories/*` | 100 requests | 1 minute | 10 |
| **ExportReports** | `/api/v1/reports/export` | 3 requests | 5 minutes | 0 |

### Rate Limiter Pipeline Registration Example

```csharp
// File: Infrastructure/Security/RateLimitingExtensions.cs
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Fingo.BackendApi.Infrastructure.Security;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddFingoRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            // Strict rate limit for Auth endpoints (Brute-force protection)
            options.AddPolicy("StrictAuth", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 2,
                        QueueLimit = 0
                    }));

            // Standard limit for authenticated API calls
            options.AddPolicy("StandardApi", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.User.FindFirst("sub")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        QueueLimit = 10
                    }));
        });

        return services;
    }
}
```

### CORS & HTTP Security Headers
1. **CORS Policy:** Restricted strictly to explicit allowed origins defined in configuration (`AllowedOrigins`). Wildcard `*` origins are banned in Staging and Production.
2. **Mandatory Security Headers Middleware:**
   - `X-Frame-Options: DENY`
   - `X-Content-Type-Options: nosniff`
   - `Referrer-Policy: strict-origin-when-cross-origin`
   - `Strict-Transport-Security: max-age=31536000; includeSubDomains` (HSTS)

### Sensitive PII & Token Scrubbing Rules
Loggers (`ILogger<T>`) **MUST NEVER** emit sensitive financial or authentication data.
The following fields are strictly redacted before logging:
- Passwords (`password`, `currentPassword`, `newPassword`)
- JWT Access & Refresh Tokens (`Bearer ...`, `refreshToken`)
- Bank Account Numbers & Credit Card Numbers

## References
- [ASP.NET Core Rate Limiting Middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [OWASP REST Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)
