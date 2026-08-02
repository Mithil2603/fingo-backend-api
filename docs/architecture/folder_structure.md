# Folder Structure & Spatial Layout Specification

## Purpose
This document provides the canonical specification of directory structures, file layout rules, and folder organization across the Fingo ASP.NET Core 8 Web API codebase.

## Scope
Applies to all source code files, configurations, migrations, docs, test suites, and infrastructure scripts within the repository.

## Contents

### Root Project Directory Layout
```
fingo-backend-api/
│
├── docs/                                  # Engineering documentation suite
│   ├── README.md                          # Central documentation index
│   ├── architecture/                      # Architectural specs & ADRs
│   ├── backend/                           # Infrastructure & component specs
│   ├── standards/                         # Coding & SQL guidelines
│   ├── setup/                             # Developer onboarding & env setup
│   └── features/                          # Domain feature specs
│
├── Features/                              # Modular Monolith feature slices
│   ├── Authentication/                    # Auth domain slice
│   │   ├── Login/                         # Login slice files
│   │   ├── Register/                      # Register slice files
│   │   ├── RefreshToken/                  # Refresh token slice files
│   │   └── Shared/                        # Shared domain primitives/models
│   ├── Accounts/                          # Bank accounts & wallets slice
│   ├── Categories/                        # Category management slice
│   ├── Transactions/                      # Financial transaction slice
│   ├── Dashboard/                         # Metrics and summaries slice
│   ├── Budgets/                           # Budgeting feature slice
│   ├── Goals/                             # Savings goals feature slice
│   ├── Reports/                           # Analytics and reporting slice
│   └── Settings/                          # User profile & preferences slice
│
├── Infrastructure/                        # Cross-cutting non-domain concerns
│   ├── Database/                          # Dapper connection factory & migrations
│   │   ├── DbConnectionFactory.cs         # PostgreSQL connection factory
│   │   ├── ApplicationDbContext.cs        # EF Core DbContext (Migrations ONLY)
│   │   └── Migrations/                    # Auto-generated EF Core migration files
│   ├── Authentication/                    # JWT generator & token validators
│   ├── Validation/                        # FluentValidation filter setup
│   ├── ErrorHandling/                     # Global exception middleware
│   ├── Responses/                         # Standardized ApiResponse envelope
│   └── Logging/                           # Serilog/ILogger extensions
│
├── Common/                                # Global constants & shared primitives
│   ├── Constants/                         # Application & route constants
│   ├── Exceptions/                        # Domain & custom exception classes
│   └── Utilities/                         # Pure utility functions
│
├── appsettings.json                      # Base configuration file
├── appsettings.Development.json          # Local developer settings
├── Program.cs                             # Composition root & middleware pipeline
├── fingo-backend-api.csproj               # .NET 8 C# project file
└── fingo-backend-api.slnx                 # Solution XML/Solution file
```

### Vertical Slice File Organization
Inside each feature folder, operations are divided by operation name. Each operation folder contains all related code:

```
Features/Transactions/CreateTransaction/
├── Endpoint.cs        # Controller action triggering the slice
├── Handler.cs         # Core business logic and Dapper SQL execution
├── Request.cs         # Strongly typed C# 12 record input DTO
├── Response.cs        # Strongly typed C# 12 record output DTO
└── Validator.cs       # FluentValidation validator class
```

### Standard Naming Rules for Feature Components
| Component | Class / File Naming Convention | Example |
| :--- | :--- | :--- |
| **Endpoint** | `<OperationName>Endpoint.cs` | `CreateTransactionEndpoint.cs` |
| **Handler** | `<OperationName>Handler.cs` | `CreateTransactionHandler.cs` |
| **Request DTO** | `<OperationName>Request.cs` | `CreateTransactionRequest.cs` |
| **Response DTO** | `<OperationName>Response.cs` | `CreateTransactionResponse.cs` |
| **Validator** | `<OperationName>Validator.cs` | `CreateTransactionValidator.cs` |

## Best Practices
- **Never place code outside feature or infrastructure slices.** Do not create top-level global folders like `Services/`, `Controllers/`, or `Repositories/`.
- Single operation files: keep `Request.cs`, `Response.cs`, `Validator.cs`, `Handler.cs`, and `Endpoint.cs` in the exact same folder representing the feature operation.
- Cross-cutting components must strictly reside in `Infrastructure/`.

## Concrete Examples

### Directory Listing Example: `Features/Accounts/GetAccountById/`

```csharp
// File: Features/Accounts/GetAccountById/Request.cs
namespace Fingo.BackendApi.Features.Accounts.GetAccountById;

public record GetAccountByIdRequest(Guid AccountId);
```

```csharp
// File: Features/Accounts/GetAccountById/Response.cs
namespace Fingo.BackendApi.Features.Accounts.GetAccountById;

public record GetAccountByIdResponse(
    Guid Id,
    string Name,
    string AccountType,
    decimal Balance,
    string Currency,
    bool IsActive,
    DateTime CreatedAt
);
```

```csharp
// File: Features/Accounts/GetAccountById/Validator.cs
using FluentValidation;

namespace Fingo.BackendApi.Features.Accounts.GetAccountById;

public class GetAccountByIdValidator : AbstractValidator<GetAccountByIdRequest>
{
    public GetAccountByIdValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");
    }
}
```

```csharp
// File: Features/Accounts/GetAccountById/Handler.cs
using Dapper;
using Fingo.BackendApi.Infrastructure.Database;

namespace Fingo.BackendApi.Features.Accounts.GetAccountById;

public class GetAccountByIdHandler
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetAccountByIdHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GetAccountByIdResponse?> HandleAsync(
        Guid userId, 
        Guid accountId, 
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                id AS Id,
                name AS Name,
                account_type AS AccountType,
                balance AS Balance,
                currency AS Currency,
                is_active AS IsActive,
                created_at AS CreatedAt
            FROM accounts
            WHERE id = @AccountId AND user_id = @UserId AND is_active = true;";

        return await connection.QueryFirstOrDefaultAsync<GetAccountByIdResponse>(
            new CommandDefinition(sql, new { AccountId = accountId, UserId = userId }, cancellationToken: cancellationToken)
        );
    }
}
```

```csharp
// File: Features/Accounts/GetAccountById/Endpoint.cs
using System.Security.Claims;
using Fingo.BackendApi.Infrastructure.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fingo.BackendApi.Features.Accounts.GetAccountById;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class GetAccountByIdEndpoint : ControllerBase
{
    private readonly GetAccountByIdHandler _handler;

    public GetAccountByIdEndpoint(GetAccountByIdHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetAccountByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Failure("Invalid auth token context."));
        }

        var account = await _handler.HandleAsync(userId, id, cancellationToken);
        if (account == null)
        {
            return NotFound(ApiResponse<object>.Failure($"Account with ID {id} was not found."));
        }

        return Ok(ApiResponse<GetAccountByIdResponse>.SuccessResponse(account));
    }
}
```

## References
- Domain-Driven Design Modular Monolith Layouts
- Vertical Slice Architecture Directory Conventions
- ASP.NET Core Project Structure Guidelines

## Notes
- Folder structure must be strictly enforced across all 9 feature domains.
- No loose scripts or miscellaneous folders are allowed at the root.
