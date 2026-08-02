# PostgreSQL & Dapper SQL Guidelines

## Purpose
This document defines SQL writing standards, parameterization security rules, indexing guidelines, transaction handling, and performance tuning rules for PostgreSQL queries in Fingo.

## Scope
Applies to all SQL queries executed via Dapper, table DDL migrations created via EF Core, and database index definitions.

## Contents

### SQL Security & Parameterization Standard
SQL injection vulnerabilities are strictly prevented by requiring **100% parameterization** for all user-supplied inputs. String concatenation or string interpolation inside SQL queries is strictly banned.

```sql
-- ❌ FORBIDDEN: String Interpolation SQL Injection Vulnerability
SELECT * FROM users WHERE email = '` + userEmail + `'

-- ✔ MANDATORY: Parameterized Dapper Query
SELECT * FROM users WHERE email = @Email;
```

### PostgreSQL Formatting Standards
1. **Keywords:** Write all SQL keywords in UPPERCASE (`SELECT`, `FROM`, `WHERE`, `JOIN`, `INSERT INTO`, `UPDATE`, `DELETE`).
2. **Identifiers:** Write all table names, column names, and aliases in `snake_case`.
3. **Multi-line Formatting:** Indent clauses cleanly to ensure readability.

### Indexing Strategy
- Primary Keys (`id`) MUST be of type `UUID` (or `gen_random_uuid()`).
- Foreign Keys (`user_id`, `account_id`, `category_id`) MUST have B-Tree indexes created (`idx_<table_name>_<column_name>`).
- Composite Indexes should be created for frequent multi-column lookup patterns (e.g., `(user_id, transaction_date)`).

## Best Practices
- Always specify explicit column lists in `SELECT` statements (`SELECT id, name, amount`). Never use `SELECT *` in production queries.
- Use PostgreSQL `RETURNING` clause when inserting records to fetch generated values in a single round trip.
- Wrap multi-statement mutations in explicit `IDbTransaction` blocks.

## Concrete Examples

### 1. Complex Query with JOINs, Aggregation, and Parameterization

```csharp
// Example Dapper handler executing production SQL query
using System.Data;
using Dapper;
using Fingo.BackendApi.Infrastructure.Database;

namespace Fingo.BackendApi.Features.Dashboard.GetMonthlySummary;

public record CategorySpendingDto(string CategoryName, string ColorHex, decimal TotalSpent);
public record MonthlySummaryResponse(decimal TotalIncome, decimal TotalExpense, List<CategorySpendingDto> SpendingBreakdown);

public class GetMonthlySummaryHandler(IDbConnectionFactory connectionFactory)
{
    public async Task<MonthlySummaryResponse> HandleAsync(Guid userId, int month, int year, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        // 1. Calculate Total Income and Total Expense for the month
        const string summarySql = @"
            SELECT 
                COALESCE(SUM(CASE WHEN transaction_type = 'Income' THEN amount ELSE 0 END), 0) AS TotalIncome,
                COALESCE(SUM(CASE WHEN transaction_type = 'Expense' THEN amount ELSE 0 END), 0) AS TotalExpense
            FROM transactions
            WHERE user_id = @UserId 
              AND EXTRACT(MONTH FROM transaction_date) = @Month 
              AND EXTRACT(YEAR FROM transaction_date) = @Year;";

        var summary = await connection.QueryFirstAsync<(decimal TotalIncome, decimal TotalExpense)>(
            new CommandDefinition(summarySql, new { UserId = userId, Month = month, Year = year }, cancellationToken: cancellationToken));

        // 2. Aggregate spending per category
        const string breakdownSql = @"
            SELECT 
                c.name AS CategoryName,
                c.color_hex AS ColorHex,
                SUM(t.amount) AS TotalSpent
            FROM transactions t
            INNER JOIN categories c ON t.category_id = c.id
            WHERE t.user_id = @UserId 
              AND t.transaction_type = 'Expense'
              AND EXTRACT(MONTH FROM t.transaction_date) = @Month 
              AND EXTRACT(YEAR FROM t.transaction_date) = @Year
            GROUP BY c.id, c.name, c.color_hex
            ORDER BY TotalSpent DESC;";

        var breakdown = await connection.QueryAsync<CategorySpendingDto>(
            new CommandDefinition(breakdownSql, new { UserId = userId, Month = month, Year = year }, cancellationToken: cancellationToken));

        return new MonthlySummaryResponse(summary.TotalIncome, summary.TotalExpense, breakdown.ToList());
    }
}
```

### 2. DDL Schema Definition Example with Indexes

```sql
-- DDL definition showing PostgreSQL conventions & indexes
CREATE TABLE transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    account_id UUID NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES categories(id) ON DELETE RESTRICT,
    amount NUMERIC(14, 2) NOT NULL CHECK (amount > 0),
    transaction_type VARCHAR(20) NOT NULL CHECK (transaction_type IN ('Income', 'Expense')),
    transaction_date TIMESTAMPTZ NOT NULL,
    description VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index creation for high-frequency queries
CREATE INDEX idx_transactions_user_id ON transactions(user_id);
CREATE INDEX idx_transactions_user_date ON transactions(user_id, transaction_date DESC);
CREATE INDEX idx_transactions_account_id ON transactions(account_id);
```

## References
- PostgreSQL Official Query Optimization Guide
- Dapper Parameterized Query Execution Performance

## Notes
- `SELECT *` is strictly forbidden in production handler code.
