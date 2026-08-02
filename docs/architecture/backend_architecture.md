# Vertical Slice Architecture & Modular Monolith Specification

## Purpose
This document defines the Vertical Slice Architecture, Modular Monolith rules, feature isolation boundaries, and component execution responsibilities for `fingo-backend-api`. **This document is the SINGLE CANONICAL OWNER of component responsibilities and data access execution rules.**

## Scope
Applies to all backend feature modules, HTTP endpoints, business handlers, data access routines, and request/response DTOs.

## Contents

### Vertical Slice Architecture Philosophy
Unlike traditional 3-layer architectures (Controller-Service-Repository) that fragment a feature across multiple horizontal projects, Fingo organizes code by **Feature Slices**.

Each feature slice contains everything necessary to fulfill a single HTTP request:

```
Features/
└── Transactions/
    └── CreateTransaction/
        ├── CreateTransactionEndpoint.cs  (HTTP Router & Protocol Mapping)
        ├── CreateTransactionHandler.cs   (Business Logic & Dapper Execution)
        ├── CreateTransactionRequest.cs   (Input DTO Record)
        ├── CreateTransactionResponse.cs  (Output DTO Record)
        └── CreateTransactionValidator.cs (FluentValidation Rules)
```

### Component Responsibilities Matrix

| Component | Allowed Responsibilities | Strictly Prohibited Responsibilities |
| :--- | :--- | :--- |
| **Endpoint** (`Endpoint.cs`) | Route mapping, HTTP verb, extracting Claims/UserId, calling Handler, returning `ApiResponse<T>`. | Database queries, business logic, manual validation. |
| **Handler** (`Handler.cs`) | Business logic, domain calculations, **direct Dapper query execution via `IDbConnectionFactory`**, transaction management. | HTTP context access, controller attributes, validation rules. |
| **Validator** (`Validator.cs`) | FluentValidation rules for Request DTO properties. | Database modifications, external API calls. |
| **Request DTO** (`Request.cs`) | Immutable input data container (`record`). | Business logic, methods. |
| **Response DTO** (`Response.cs`) | Immutable output data container (`record`). | Database entity annotations, entity references. |

### Data Access Policy (Handler-Direct Dapper Queries)
To eliminate boilerplate pass-through abstractions, **Generic Repositories and Unit of Work patterns are strictly prohibited**.
- Handlers interact directly with PostgreSQL using Dapper via `IDbConnectionFactory`.
- SQL queries sit directly inside the Handler or a dedicated `SqlQueries` string constant file in the same feature folder.
- Repositories are NOT created for standard feature slices.

```csharp
// File: Features/Transactions/CreateTransaction/CreateTransactionHandler.cs
using Dapper;
using Fingo.BackendApi.Infrastructure.Database;

namespace Fingo.BackendApi.Features.Transactions.CreateTransaction;

public class CreateTransactionHandler
{
    private readonly IDbConnectionFactory _factory;

    public CreateTransactionHandler(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<CreateTransactionResponse> HandleAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        using var connection = _factory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string sql = @"
                INSERT INTO transactions (id, user_id, account_id, category_id, amount, description, transaction_date, created_at)
                VALUES (@Id, @UserId, @AccountId, @CategoryId, @Amount, @Description, @TransactionDate, NOW())
                RETURNING id, amount, created_at AS CreatedAt;";

            var result = await connection.QuerySingleAsync<CreateTransactionResponse>(
                new CommandDefinition(sql, new { 
                    Id = Guid.NewGuid(), 
                    UserId = userId, 
                    request.AccountId, 
                    request.CategoryId, 
                    request.Amount, 
                    request.Description, 
                    request.TransactionDate 
                }, transaction: transaction, cancellationToken: cancellationToken)
            );

            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

## References
- [Project Decisions ADR](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/architecture/project_decisions.md)
- [Database Specification](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/database.md)
