# Savings Goals Feature Specification

## Purpose
This document defines the functional behavior, table schema, execution workflow, and API contracts for user savings goals (e.g. Vacation Fund, Emergency Reserve) in Fingo.

## Business Rules
1. A goal has a title, target amount, current amount, and target deadline date.
2. Users can make contributions to or withdrawals from a goal.
3. Goal progress percentage = `(current_amount / target_amount) * 100`.
4. Goals are completed when `current_amount >= target_amount`.

## User Stories
- **US-GOAL-01:** As a user, I want to define a financial goal (e.g., "Emergency Fund: $10,000 by Dec 2026").
- **US-GOAL-02:** As a user, I want to log contributions towards my goal.
- **US-GOAL-03:** As a user, I want to see visual progress towards achieving my savings goals.

## Screens
- Savings Goals List Screen (`/goals`)
- Add/Edit Goal Modal (`/goals/modal`)
- Goal Contribution Modal (`/goals/:id/contribute`)

## Navigation Flow
```
[ Dashboard ] ---> Click "Goals" ---> [ Savings Goals Screen ]
                                             |
                          +------------------+------------------+
                          |                                     |
                Click "Create Goal"                   Click "Add Funds"
                          v                                     v
                 [ Add Goal Modal ]                  [ Contribution Modal ]
```

## Database Tables

### 1. `goals` Table Schema
```sql
CREATE TABLE goals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(150) NOT NULL,
    target_amount NUMERIC(14, 2) NOT NULL CHECK (target_amount > 0),
    current_amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00 CHECK (current_amount >= 0),
    target_date TIMESTAMPTZ NOT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_goals_user_id ON goals(user_id);
```

### 2. `goal_contributions` Table Schema
```sql
CREATE TABLE goal_contributions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    goal_id UUID NOT NULL REFERENCES goals(id) ON DELETE CASCADE,
    account_id UUID REFERENCES accounts(id) ON DELETE SET NULL,
    amount NUMERIC(14, 2) NOT NULL CHECK (amount > 0),
    contribution_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    note VARCHAR(250)
);

CREATE INDEX idx_goal_contributions_goal_id ON goal_contributions(goal_id);
```

## Relationships
- `users` (1) <----> (N) `goals`
- `goals` (1) <----> (N) `goal_contributions`

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/goals` | Retrieves all savings goals for authenticated user | Authorized |
| `POST` | `/api/goals` | Creates a new savings goal | Authorized |
| `POST` | `/api/goals/{id}/contributions` | Adds a contribution to a savings goal | Authorized |
| `DELETE` | `/api/goals/{id}` | Deletes a savings goal | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Goals.CreateGoal;

public record CreateGoalRequest(
    string Title,
    decimal TargetAmount,
    DateTime TargetDate
);
```

```csharp
namespace Fingo.BackendApi.Features.Goals.AddContribution;

public record AddContributionRequest(
    Guid? AccountId,
    decimal Amount,
    string? Note
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Goals.Shared;

public record GoalDto(
    Guid Id,
    string Title,
    decimal TargetAmount,
    decimal CurrentAmount,
    double ProgressPercentage,
    DateTime TargetDate,
    bool IsCompleted,
    DateTime CreatedAt
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Goals.CreateGoal;

public class CreateGoalValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
        RuleFor(x => x.TargetDate).GreaterThan(DateTime.UtcNow);
    }
}
```

## Authorization
- Goal management endpoints verify ownership using `WHERE user_id = @UserId`.

## Business Logic
1. **Add Contribution Handler:**
   - Opens `IDbTransaction`.
   - Inserts record into `goal_contributions`.
   - Updates goal progress: `UPDATE goals SET current_amount = current_amount + @Amount, is_completed = (current_amount + @Amount >= target_amount) WHERE id = @GoalId`.
   - Optional: If `AccountId` is provided, creates corresponding expense transaction in that account.
   - Commits transaction.

## Edge Cases
- **Contribution Exceeds Target:** Allowed (Goal completes and marks `is_completed = true`).

## Error Scenarios
- **Goal Not Found:** Returns HTTP 404.

## Future Improvements
- Automated micro-savings round-up integrations.

## Checklists

### Definition of Done
- [x] Vertical slice implemented for goals and contributions.
- [x] Progress calculations verified.

### Testing Checklist
- [x] Test contributing updates `current_amount` and checks `is_completed`.

## Dependencies
- Dapper, PostgreSQL, `IDbConnectionFactory`.

## Related Documents
- [Accounts Feature Spec](accounts.md)
- [Dashboard Feature Spec](dashboard.md)
