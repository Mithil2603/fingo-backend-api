# Standard API Response Envelope Specification

## Purpose
This document defines the unified API response model, payload structure, pagination container, and error formatting enforced across all Fingo Web API endpoints. **This document is the SINGLE CANONICAL OWNER of API response formats.**

## Scope
Applies to all HTTP endpoint responses, controller actions, exception middleware outputs, and validation error payloads returned by the Fingo backend.

## Contents

### Principles of Unified Response Structure
To ensure consistent client-side consumption by Flutter and web clients, Fingo strictly enforces a standard response envelope `ApiResponse<T>`.

Endpoints MUST NEVER return raw unstructured models, raw strings, or untyped dynamic JSON objects. Every response must match the canonical schema structure:

#### 1. Success Response Structure
```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Checking Account",
    "balance": 1500.50
  },
  "errors": null,
  "timestamp": "2026-08-02T16:50:00Z"
}
```

#### 2. Error & Validation Failure Structure (Canonical Dictionary Format)
Validation and runtime errors are mapped to field-keyed arrays to provide unambiguous error context:
```json
{
  "success": false,
  "message": "Validation failed for request payload.",
  "data": null,
  "errors": {
    "email": [
      "Email is required.",
      "Invalid email address format."
    ],
    "password": [
      "Password must be at least 8 characters long."
    ]
  },
  "timestamp": "2026-08-02T16:50:00Z"
}
```

For general unhandled exceptions or domain errors not tied to a specific field:
```json
{
  "success": false,
  "message": "Insufficient funds in source account.",
  "data": null,
  "errors": {
    "general": [
      "Insufficient funds in source account."
    ]
  },
  "timestamp": "2026-08-02T16:50:00Z"
}
```

### Paginated Response Structure (`PagedData<T>`)
For query endpoints returning lists of items (e.g., transactions, logs), the response data property embeds pagination metadata:

```json
{
  "success": true,
  "message": "Transactions retrieved successfully.",
  "data": {
    "items": [ ... ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalRecords": 142,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "errors": null,
  "timestamp": "2026-08-02T16:50:00Z"
}
```

## Best Practices
- Always return HTTP status codes matching semantic result: `200 OK` for success, `201 Created` for creations, `400 Bad Request` for validation failures, `401 Unauthorized` for auth failures, `404 Not Found` for missing resources, `429 Too Many Requests` for rate limits, `500 Internal Server Error` for unhandled exceptions.
- Do NOT nest `ApiResponse<T>` inside another envelope.
- Ensure proper serialization using `System.Text.Json` camelCase property naming.

## Concrete Implementation Example

```csharp
// File: Infrastructure/Responses/ApiResponse.cs
using System.Text.Json.Serialization;

namespace Fingo.BackendApi.Infrastructure.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Failure(string message, Dictionary<string, string[]>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? new Dictionary<string, string[]>
            {
                { "general", new[] { message } }
            },
            Timestamp = DateTime.UtcNow
        };
    }
}
```

## References
- [Validation Specification](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/validation.md)
- [Error Handling Specification](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/error_handling.md)
