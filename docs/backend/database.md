# Database Architecture & Data Access Specification

## Purpose
This document defines the database architecture, connection factory design, Dapper query rules, transaction handling, PostgreSQL conventions, and EF Core migration usage for Fingo.

## Scope
Covers database configuration, Npgsql provider integration, Dapper query mappings, database migrations, connection lifecycle, and performance tuning.

## Contents

### Relational Database Engine
Fingo relies on **PostgreSQL 16+** as its primary relational database engine. PostgreSQL was selected for its reliability, JSONB support, strict ACID compliance, and robust index types (B-Tree, GIN, Hash).

### Dapper Micro-ORM Execution Model
All application queries and data updates execute using **Dapper**. Dapper maps SQL query parameters and maps result sets directly to C# 12 records or classes.

```
+-----------------------------------------------------------------------+
|                       DAPPER EXECUTION PIPELINE                       |
|                                                                       |
|  +------------------------+      +---------------------------------+  |
|  | NpgsqlConnection       | ---> | IDbConnectionFactory            |  |
|  | (PostgreSQL Provider)  |      | (.CreateConnection())           |  |
|  +------------------------+      +---------------------------------+  |
|                                                  |                    |
|                                                  v                    |
|  +------------------------+      +---------------------------------+  |
|  | Parametrized SQL       | ---> | Dapper QueryAsync / ExecuteAsync|  |
|  | (@UserId, @Amount)     |      | (Strongly Typed DTO Binding)    |  |
|  +------------------------+      +---------------------------------+  |
+-----------------------------------------------------------------------+
```

### Database Connection Factory (`IDbConnectionFactory`)
To avoid leaky abstractions, Fingo uses a lightweight `IDbConnectionFactory` interface that yields openable `IDbConnection` instances tied to Npgsql.

### Database Migrations via EF Core (Migrations ONLY)
Entity Framework Core is used strictly as a database schema definition and migration generator tool.
- EF Core `DbContext` contains entity mappings matching database tables.
- **NO EF Core CRUD operations** (`.Add()`, `.Update()`, `.SaveChanges()`, `.ToList()`) are permitted in feature slices or handlers.
- Command for creating migration: `dotnet ef migrations add <MigrationName>`
- Command for applying migration: `dotnet ef database update`

## Best Practices
- Always encapsulate multi-table write operations inside an explicit `IDbTransaction`.
- Use snake_case column names in PostgreSQL and alias them to PascalCase property names in SQL queries (`SELECT user_id AS UserId`).
- Pass `CancellationToken` into all Dapper query commands via `CommandDefinition`.
- Dispose connection and transaction objects using C# `using` blocks.

## Concrete Examples

### 1. Database Connection Factory Interface & Implementation

```csharp
// File: Infrastructure/Database/IDbConnectionFactory.cs
using System.Data;

namespace Fingo.BackendApi.Infrastructure.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
```

```csharp
// File: Infrastructure/Database/DbConnectionFactory.cs
using System.Data;
using Npgsql;

namespace Fingo.BackendApi.Infrastructure.Database;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' is missing.");
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
```

### 2. Dapper Query with Transaction & Mapping Example

```csharp
// Example: Complex multi-table Dapper execution in a Handler
using System.Data;
using Dapper;
using Fingo.BackendApi.Infrastructure.Database;

namespace Fingo.BackendApi.Features.Transactions.CreateTransfer;

public record CreateTransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Description,
    DateTime TransferDate
);

public record CreateTransferResponse(
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    DateTime CreatedAt
);

public class CreateTransferHandler
{
    private readonly IDbConnectionFactory _factory;

    public CreateTransferHandler(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<CreateTransferResponse> HandleAsync(Guid userId, CreateTransferRequest request, CancellationToken cancellationToken)
    {
        using var connection = _factory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Verify source balance
            const string balanceCheckSql = @"
                SELECT balance FROM accounts 
                WHERE id = @SourceAccountId AND user_id = @UserId AND is_active = true FOR UPDATE;";

            var sourceBalance = await connection.QueryFirstOrDefaultAsync<decimal?>(
                new CommandDefinition(balanceCheckSql, new { request.SourceAccountId, UserId = userId }, transaction: transaction, cancellationToken: cancellationToken));

            if (sourceBalance == null)
            {
                throw new InvalidOperationException("Source account not found or access denied.");
            }

            if (sourceBalance < request.Amount)
            {
                throw new InvalidOperationException("Insufficient funds for transfer.");
            }

            // Deduct from source account
            const string deductSql = @"
                UPDATE accounts SET balance = balance - @Amount, updated_at = NOW() 
                WHERE id = @SourceAccountId;";
            await connection.ExecuteAsync(new CommandDefinition(deductSql, new { request.Amount, request.SourceAccountId }, transaction: transaction, cancellationToken: cancellationToken));

            // Credit to destination account
            const string creditSql = @"
                UPDATE accounts SET balance = balance + @Amount, updated_at = NOW() 
                WHERE id = @DestinationAccountId;";
            await connection.ExecuteAsync(new CommandDefinition(creditSql, new { request.Amount, request.DestinationAccountId }, transaction: transaction, cancellationToken: cancellationToken));

            // Insert transfer record
            var transferId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            const string insertTransferSql = @"
                INSERT INTO transfers (id, user_id, source_account_id, destination_account_id, amount, description, created_at)
                VALUES (@TransferId, @UserId, @SourceAccountId, @DestinationAccountId, @Amount, @Description, @CreatedAt);";

            await connection.ExecuteAsync(new CommandDefinition(
                insertTransferSql,
                new { TransferId = transferId, UserId = userId, request.SourceAccountId, request.DestinationAccountId, request.Amount, request.Description, CreatedAt = createdAt },
                transaction: transaction,
                cancellationToken: cancellationToken
            ));

            transaction.Commit();

            return new CreateTransferResponse(transferId, request.SourceAccountId, request.DestinationAccountId, request.Amount, createdAt);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

### 3. Migration-Only EF Core DbContext Definition

```csharp
// File: Infrastructure/Database/ApplicationDbContext.cs
// NOTE: Used ONLY by EF Core Migration Tool CLI. Never inject into handlers for CRUD!
using Microsoft.EntityFrameworkCore;

namespace Fingo.BackendApi.Infrastructure.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Define table schemas for migrations
        modelBuilder.Entity<UserEntity>(entity => {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}

public class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

## References
- PostgreSQL Official Documentation
- Dapper GitHub & Performance Benchmarks
- EF Core CLI Tools Migration Guide

## Notes
- Direct EF Core queries are strictly banned in all feature modules.
