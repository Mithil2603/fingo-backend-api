# Structured Logging & Telemetry Specification

## Purpose
This document defines the structured logging infrastructure, message formatting rules, log levels, correlation tokens, and `ILogger<T>` integration across the Fingo backend.

## Scope
Covers application log events, infrastructure logging, HTTP request telemetry, database execution logging, and log level policies.

## Contents

### Structured Logging Architecture
Fingo strictly prohibits unstructured text output (such as `Console.WriteLine()` or manual string concatenation inside log calls).

All log events utilize **Structured Logging** using `Microsoft.Extensions.Logging.ILogger<T>`.

Structured logging captures parameters as named key-value pairs rather than static strings. This allows log aggregators (Serilog, Elasticsearch, Datadog) to parse, filter, and index telemetry metrics instantly.

```
+-----------------------------------------------------------------------------------+
|                            STRUCTURED LOGGING PIPELINE                            |
|                                                                                   |
|  [ Endpoint / Handler ]                                                           |
|         |                                                                         |
|         v                                                                         |
|  _logger.LogInformation("Processed transaction {TransactionId} for user {UserId}",|
|                          transactionId, userId);                                  |
|         |                                                                         |
|         v                                                                         |
|  +-----------------------------------------------------------------------------+  |
|  | Structured JSON Log Event Output:                                           |  |
|  | {                                                                           |  |
|  |   "Timestamp": "2026-08-02T16:50:00Z",                                      |  |
|  |   "LogLevel": "Information",                                                |  |
|  |   "MessageTemplate": "Processed transaction {TransactionId} for user {UserId}",|
|  |   "Properties": {                                                           |  |
|  |     "TransactionId": "a1b2c3d4-...",                                        |  |
|  |     "UserId": "e5f6a7b8-..."                                                |  |
|  |   }                                                                         |  |
|  | }                                                                           |  |
|  +-----------------------------------------------------------------------------+  |
+-----------------------------------------------------------------------------------+
```

### Log Levels Policy

| Log Level | Usage Criterion | Example Event |
| :--- | :--- | :--- |
| **Trace** | Detailed internal diagnostic tracing for development debugging. | Low-level byte parser execution. |
| **Debug** | Internal execution details useful for developer troubleshooting. | Dapper SQL query parameter bindings. |
| **Information** | Normal application milestone events, state changes, user activities. | User logged in, Transaction created, Report generated. |
| **Warning** | Unexpected non-fatal conditions, degraded operations, security soft alerts. | Invalid login attempt, Rate limit threshold warning. |
| **Error** | Handled exceptions, operation failures affecting user requests. | Database connection attempt failed, Payment gateway failure. |
| **Critical** | Fatal application failures requiring immediate engineering intervention. | Database corrupted, Out of memory, Master config missing. |

## Best Practices
- Never use string interpolation (`$"User {userId} failed"`) inside logger method calls; always use message templates (`"User {UserId} failed", userId`).
- Never log sensitive payload attributes (passwords, credit card numbers, JWT secrets, refresh tokens).
- Inject `ILogger<TClass>` using primary constructors.

## Concrete Examples

### 1. Handler Class Structured Logging Implementation

```csharp
// File: Features/Authentication/Login/Handler.cs
using Fingo.BackendApi.Infrastructure.Authentication;
using Fingo.BackendApi.Infrastructure.Database;
using Dapper;

namespace Fingo.BackendApi.Features.Authentication.Login;

public class LoginHandler
{
    private readonly IDbConnectionFactory _factory;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IDbConnectionFactory factory,
        IPasswordHasherService passwordHasher,
        IJwtProvider jwtProvider,
        ILogger<LoginHandler> logger)
    {
        _factory = factory;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _logger = logger;
    }

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting login authentication for user email: {Email}", request.Email);

        using var connection = _factory.CreateConnection();
        const string sql = "SELECT id, email, password_hash FROM users WHERE email = @Email;";

        var user = await connection.QueryFirstOrDefaultAsync<UserDbRecord>(
            new CommandDefinition(sql, new { request.Email }, cancellationToken: cancellationToken));

        if (user == null)
        {
            _logger.LogWarning("Login failed. User email not found: {Email}", request.Email);
            throw new InvalidOperationException("Invalid email or password.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed. Invalid password for UserId: {UserId}, Email: {Email}", user.Id, request.Email);
            throw new InvalidOperationException("Invalid email or password.");
        }

        var accessToken = _jwtProvider.GenerateAccessToken(user.Id, user.Email);
        var refreshToken = _jwtProvider.GenerateRefreshToken();

        _logger.LogInformation("User authentication successful for UserId: {UserId}", user.Id);

        return new LoginResponse(accessToken, refreshToken, user.Id, user.Email);
    }
}

public record UserDbRecord(Guid Id, string Email, string PasswordHash);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, Guid UserId, string Email);
```

### 2. HTTP Request Correlation Middleware (`CorrelationIdMiddleware.cs`)

```csharp
// File: Infrastructure/Logging/CorrelationIdMiddleware.cs
namespace Fingo.BackendApi.Infrastructure.Logging;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
```

## References
- High-Performance Logging in .NET 8 (LoggerMessageAttribute)
- Structured Logging Best Practices with ILogger

## Notes
- Console.WriteLine is strictly forbidden throughout the solution.
