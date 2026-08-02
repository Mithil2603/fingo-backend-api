# Budgets Feature Specification

## Purpose
This document specifies the technical design, database model, calculation engine, and API endpoints for category budget tracking in Fingo.

## Business Rules
1. Budgets are defined per category for a specific calendar month and year.
2. A category can have only one active budget target per month (`UNIQUE(user_id, category_id, month, year)`).
3. The spent amount is dynamically calculated from expense transactions linked to the budget category within that month.
4. Remaining budget = `target_amount - spent_amount`.
5. Progress percentage = `(spent_amount / target_amount) * 100`.

## User Stories
- **US-BUD-01:** As a user, I want to set a monthly spending budget target for specific expense categories (e.g., $400 for Dining Out in August).
- **US-BUD-02:** As a user, I want to view my budget progress progress bar (green < 80%, yellow 80-99%, red >= 100%).
- **US-BUD-03:** As a user, I want to update or delete a budget target.

## Screens
- Monthly Budgets Overview Screen (`/budgets`)
- Add/Edit Budget Modal (`/budgets/modal`)

## Navigation Flow
```
[ Dashboard ] ---> Click "Budgets" ---> [ Budgets Overview Screen ]
                                               |
                                     (Select Month/Year Filter)
                                               v
                                    [ Budget Target Cards ]
```

## Database Tables

### `budgets` Table Schema
```sql
CREATE TABLE budgets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    target_amount NUMERIC(14, 2) NOT NULL CHECK (target_amount > 0),
    month INT NOT NULL CHECK (month BETWEEN 1 AND 12),
    year INT NOT NULL CHECK (year >= 2024),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_budgets_user_category_period UNIQUE (user_id, category_id, month, year)
);

CREATE INDEX idx_budgets_user_period ON budgets(user_id, year, month);
```

## Relationships
- `users` (1) <----> (N) `budgets`
- `categories` (1) <----> (N) `budgets`

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/budgets` | Gets budget target list with spent progress for specified month/year | Authorized |
| `POST` | `/api/budgets` | Creates a new monthly category budget target | Authorized |
| `PUT` | `/api/budgets/{id}` | Updates budget target amount | Authorized |
| `DELETE` | `/api/budgets/{id}` | Deletes a budget target | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Budgets.CreateBudget;

public record CreateBudgetRequest(
    Guid CategoryId,
    decimal TargetAmount,
    int Month,
    int Year
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Budgets.Shared;

public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryColorHex,
    decimal TargetAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    double PercentageUsed,
    int Month,
    int Year,
    bool IsExceeded
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Budgets.CreateBudget;

public class CreateBudgetValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID is required.");
        RuleFor(x => x.TargetAmount).GreaterThan(0).WithMessage("Target amount must be greater than zero.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");
        RuleFor(x => x.Year).GreaterThanOrEqualTo(2024).WithMessage("Year must be 2024 or later.");
    }
}
```

## Authorization
- Budget endpoints enforce strict ownership matching authenticated user ID.

## Business Logic
1. **Get Budgets Handler:**
   - Queries `budgets` table joined with `categories` and dynamically sums matching `transactions`:
   ```sql
   SELECT 
       b.id AS Id,
       b.category_id AS CategoryId,
       c.name AS CategoryName,
       c.color_hex AS CategoryColorHex,
       b.target_amount AS TargetAmount,
       COALESCE(SUM(t.amount), 0) AS SpentAmount,
       b.month AS Month,
       b.year AS Year
   FROM budgets b
   INNER JOIN categories c ON b.category_id = c.id
   LEFT JOIN transactions t ON t.category_id = b.category_id 
       AND t.user_id = b.user_id 
       AND t.transaction_type = 'Expense'
       AND t.is_deleted = false
       AND EXTRACT(MONTH FROM t.transaction_date) = b.month 
       AND EXTRACT(YEAR FROM t.transaction_date) = b.year
   WHERE b.user_id = @UserId AND b.month = @Month AND b.year = @Year
   GROUP BY b.id, b.category_id, c.name, c.color_hex, b.target_amount, b.month, b.year;
   ```
   - Computes derived properties (`RemainingAmount`, `PercentageUsed`, `IsExceeded`).

## Edge Cases
- **Duplicate Period Budget:** Database constraint returns 409 Conflict.

## Error Scenarios
- **Budget Not Found:** Returns HTTP 404.

## Future Improvements
- Auto-rollover unspent budget amounts to next month.

## Checklists

### Definition of Done
- [x] Full Vertical Slice for budget management.
- [x] Spent aggregation query tested against actual transactions.

### Testing Checklist
- [x] Test percentage calculation when spent exceeds target.

## Dependencies
- Dapper, PostgreSQL, `IDbConnectionFactory`.

## Related Documents
- [Categories Feature Spec](categories.md)
- [Transactions Feature Spec](transactions.md)
