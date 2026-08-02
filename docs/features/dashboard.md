# Dashboard Feature Specification

## Purpose
This document specifies the metrics aggregation, card summaries, recent transaction widgets, and net worth calculations powering the main dashboard screen in Fingo.

## Business Rules
1. Dashboard metrics reflect real-time calculations aggregated over user accounts and transactions.
2. Net worth equals total active account balances (`SUM(balance)` across checking, savings, cash, investments minus credit card debts).
3. Monthly income and expense totals reflect transactions recorded in the current active calendar month.
4. Recent activity widget displays the 5-10 most recent transactions sorted by `transaction_date DESC`.

## User Stories
- **US-DASH-01:** As a user, I want to see my total net worth and cash flow overview immediately upon logging in.
- **US-DASH-02:** As a user, I want to view a monthly spending breakdown pie chart by top categories.
- **US-DASH-03:** As a user, I want a quick widget showing recent transactions and account balances.

## Screens
- Main Dashboard Screen (`/dashboard`)

## Navigation Flow
```
[ Login ] ---> [ Dashboard Screen ]
                    |
      +-------------+-------------+
      |                           |
Click "Recent Tx"          Click "View Accounts"
      v                           v
[ Transaction Ledger ]     [ Accounts Screen ]
```

## Database Tables
- Aggregates data across `accounts`, `transactions`, `categories`, and `budgets` tables.

## Relationships
- Read-only aggregation model over existing domain entity tables.

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/dashboard/summary` | Gets high-level dashboard financial summary | Authorized |
| `GET` | `/api/dashboard/spending-breakdown` | Gets category breakdown pie chart data | Authorized |

## Request DTOs
- Query parameters optional (e.g. `?month=8&year=2026`). Defaults to current month/year.

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Dashboard.GetDashboardSummary;

public record DashboardSummaryResponse(
    decimal TotalNetWorth,
    decimal MonthlyIncome,
    decimal MonthlyExpense,
    decimal NetSavings,
    List<RecentTransactionDto> RecentTransactions,
    List<AccountSummaryDto> AccountSummaries
);

public record RecentTransactionDto(
    Guid Id,
    string CategoryName,
    string CategoryColorHex,
    string AccountName,
    decimal Amount,
    string TransactionType,
    DateTime TransactionDate
);

public record AccountSummaryDto(
    Guid AccountId,
    string AccountName,
    string AccountType,
    decimal Balance,
    string Currency
);
```

```csharp
namespace Fingo.BackendApi.Features.Dashboard.GetSpendingBreakdown;

public record SpendingBreakdownResponse(
    int Month,
    int Year,
    decimal TotalSpending,
    List<CategorySpendingItemDto> Categories
);

public record CategorySpendingItemDto(
    Guid CategoryId,
    string CategoryName,
    string ColorHex,
    decimal AmountSpent,
    double Percentage
);
```

## Validation Rules
- Validates query parameter ranges if provided (`Month` between 1-12, `Year` >= 2024).

## Authorization
- Requires valid JWT authorization. Aggregates data strictly filtered by authenticated user's ID (`WHERE user_id = @UserId`).

## Business Logic
1. **Dashboard Summary Handler:**
   - Executes multi-result Dapper query using `QueryMultipleAsync`:
     - Query 1: Total Net Worth (`SELECT COALESCE(SUM(balance), 0) FROM accounts WHERE user_id = @UserId AND is_active = true`).
     - Query 2: Current Month Cash Flow (`SELECT SUM(CASE WHEN transaction_type = 'Income' THEN amount ELSE 0 END), SUM(CASE WHEN transaction_type = 'Expense' THEN amount ELSE 0 END) FROM transactions ...`).
     - Query 3: Top 5 Recent Transactions (`SELECT t.id, c.name, ... LIMIT 5`).
     - Query 4: Active Account Balances.
   - Combines query results into single `DashboardSummaryResponse`.

## Edge Cases
- **New User with Zero Accounts:** Returns `$0.00` net worth, empty arrays for recent transactions and account summaries without crashing.

## Error Scenarios
- **Invalid Date Query Parameters:** Returns HTTP 400 Bad Request.

## Future Improvements
- Customizable widget grid layout on frontend.

## Checklists

### Definition of Done
- [x] Multi-grid Dapper query optimized for fast sub-50ms execution.
- [x] Response envelope verified in Swagger.

### Testing Checklist
- [x] Test empty database state returns zero values cleanly.
- [x] Test net worth updates immediately after transaction additions.

## Dependencies
- Dapper, PostgreSQL, `IDbConnectionFactory`.

## Related Documents
- [Accounts Feature Spec](accounts.md)
- [Transactions Feature Spec](transactions.md)
