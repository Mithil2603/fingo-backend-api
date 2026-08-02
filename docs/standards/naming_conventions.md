# Naming Conventions & Identifier Rules

## Purpose
This document establishes the precise naming conventions across C# code, API HTTP routes, database entities, and configuration keys in Fingo.

## Scope
Applies to namespaces, classes, interfaces, methods, properties, variables, PostgreSQL tables, columns, indexes, and REST URL endpoints.

## Contents

### Master Naming Matrix

| Target Context | Casing Style | Example | Rule / Notes |
| :--- | :--- | :--- | :--- |
| **Classes & Records** | `PascalCase` | `CreateTransactionHandler` | Descriptive noun or verb-noun phrase |
| **Interfaces** | `PascalCase` with `I` prefix | `IDbConnectionFactory` | Prefix with capital `I` |
| **Methods** | `PascalCase` | `HandleAsync`, `CreateConnection` | Verb phrase, append `Async` for async methods |
| **Properties** | `PascalCase` | `TransactionDate`, `Amount` | Matching C# standard property capitalization |
| **Method Parameters** | `camelCase` | `userId`, `cancellationToken` | camelCase identifier |
| **Private Fields** | `_camelCase` | `_connectionFactory`, `_logger` | Leading underscore followed by camelCase |
| **API Endpoints (Routes)** | `kebab-case` | `/api/user-profiles`, `/api/transactions` | Lowercase hyphenated words |
| **PostgreSQL Tables** | `snake_case` (Plural) | `users`, `accounts`, `transactions` | Pluralized lowercase snake_case |
| **PostgreSQL Columns** | `snake_case` (Singular) | `user_id`, `created_at`, `amount` | Lowercase snake_case |
| **PostgreSQL Foreign Keys** | `snake_case` | `account_id`, `category_id` | Terminate with `_id` suffix |
| **PostgreSQL Primary Key** | `snake_case` | `id` | Standard `id` (UUID/Guid) |
| **JSON Payload Keys** | `camelCase` | `"transactionDate"`, `"amount"` | System.Text.Json CamelCase naming policy |

## Best Practices
- Never use abbreviations unless universally recognized (`Id`, `Url`, `Jwt`, `Sql`).
- API route parameters must be lower-case or kebab-cased (`/api/accounts/{accountId}`).
- Standardize slice file names strictly: `Endpoint.cs`, `Handler.cs`, `Request.cs`, `Response.cs`, `Validator.cs`.

## Concrete Examples

### 1. API Route & DTO Binding Naming Alignment

```csharp
// Example demonstrating alignment across HTTP Route, DTO properties, and PostgreSQL mapping
namespace Fingo.BackendApi.Features.Accounts.UpdateAccount;

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/accounts")] // Route: lowercase kebab-case
public class UpdateAccountEndpoint : ControllerBase
{
    private readonly UpdateAccountHandler _handler;

    public UpdateAccountEndpoint(UpdateAccountHandler handler)
    {
        _handler = handler;
    }

    [HttpPut("{accountId:guid}")] // Route parameter: camelCase
    public async Task<IActionResult> UpdateAccount(
        [FromRoute] Guid accountId,
        [FromBody] UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _handler.HandleAsync(userId, accountId, request, cancellationToken);
        return Ok(result);
    }
}

// Request DTO: PascalCase properties
public record UpdateAccountRequest(
    string Name,
    string AccountType,
    string Currency
);
```

### 2. SQL Column Alias Mapping to C# PascalCase Properties

```sql
-- Dapper SQL Query demonstrating snake_case DB columns aliased to PascalCase C# properties
SELECT 
    id AS Id,
    user_id AS UserId,
    account_id AS AccountId,
    category_id AS CategoryId,
    amount AS Amount,
    transaction_type AS TransactionType,
    transaction_date AS TransactionDate,
    created_at AS CreatedAt
FROM transactions
WHERE user_id = @UserId AND is_deleted = false;
```

## References
- C# Coding Conventions (Microsoft Docs)
- PostgreSQL Naming Conventions Standard Guide

## Notes
- Consistency is strictly required across all 9 domain feature modules.
