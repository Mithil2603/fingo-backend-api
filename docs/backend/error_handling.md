# Global Error Handling & Exception Middleware Specification

## Purpose
This document defines the central exception handling architecture, HTTP status code mapping, database error translation, and security masking for Fingo.

## Scope
Applies to all unhandled exceptions occurring within the HTTP request pipeline, background services, and database queries.

## Contents

### Exception Handling Architecture
Fingo uses a global `ExceptionHandlingMiddleware` to catch all uncaught runtime exceptions and format them into the standard `ApiResponse<T>` envelope defined in [api_response.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/api_response.md).

Controllers and Handlers **MUST NOT** wrap code blocks in generic `try-catch` statements unless performing explicit transaction rollbacks or catching specific recoverable domain exceptions.

### HTTP Status Code & Database Exception Mapping Matrix

| Exception Type | Trigger Cause | HTTP Status Code | Response Message |
| :--- | :--- | :--- | :--- |
| `ValidationException` | Payload failed FluentValidation rules | `400 Bad Request` | "Validation failed." |
| `PostgresException (23505)` | Unique constraint violation (e.g. duplicate email) | `409 Conflict` | "A record with this unique identifier already exists." |
| `PostgresException (23503)` | Foreign key constraint violation | `400 Bad Request` | "Referenced entity does not exist." |
| `KeyNotFoundException` | Resource missing for given ID | `404 Not Found` | "Requested resource was not found." |
| `UnauthorizedAccessException` | Missing or invalid auth token | `401 Unauthorized` | "Authentication required." |
| `InvalidOperationException` | Domain rule breach (e.g. insufficient funds) | `400 Bad Request` | Domain exception message |
| `Exception` (Generic) | Unhandled system fault | `500 Internal Server Error` | "An unexpected error occurred." |

### Global Exception Middleware Implementation

```csharp
// File: Infrastructure/Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using Fingo.BackendApi.Infrastructure.Responses;
using Npgsql;

namespace Fingo.BackendApi.Infrastructure.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception caught by global middleware: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errorKey) = exception switch
        {
            PostgresException pgEx when pgEx.SqlState == "23505" => 
                (HttpStatusCode.Conflict, "A record with this unique identifier already exists.", "duplicateKey"),
            PostgresException pgEx when pgEx.SqlState == "23503" => 
                (HttpStatusCode.BadRequest, "Referenced entity does not exist.", "foreignKeyViolation"),
            KeyNotFoundException => 
                (HttpStatusCode.NotFound, "Requested resource was not found.", "notFound"),
            UnauthorizedAccessException => 
                (HttpStatusCode.Unauthorized, "Authentication required.", "unauthorized"),
            InvalidOperationException invEx => 
                (HttpStatusCode.BadRequest, invEx.Message, "invalidOperation"),
            _ => 
                (HttpStatusCode.InternalServerError, "An internal server error occurred.", "internalServerError")
        };

        context.Response.StatusCode = (int)statusCode;

        var errors = new Dictionary<string, string[]>
        {
            { errorKey, new[] { message } }
        };

        var response = ApiResponse<object>.Failure(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return context.Response.WriteAsync(json);
    }
}
```

## References
- [Standard API Response Specification](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/api_response.md)
- [PostgreSQL Error Codes (Appendix A)](https://www.postgresql.org/docs/current/errcodes-appendix.html)
