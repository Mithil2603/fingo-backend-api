# Accounts Feature Specification

## Purpose
This document specifies the technical architecture, database design, endpoints, and business rules for bank accounts, cash wallets, and credit cards in Fingo.

## Business Rules
1. Every user can own multiple accounts (e.g., Checking, Savings, Cash Wallet, Credit Card).
2. Account balance is updated automatically whenever income, expense, or transfer transactions occur.
3. Deleting an account performs a soft-delete (`is_active = false`) to preserve transaction ledger integrity.
4. Account names must be unique per user (`UNIQUE(user_id, name)`).
5. Currency codes must follow standard ISO 4217 (e.g., USD, EUR, INR, GBP).

## User Stories
- **US-ACC-01:** As a user, I want to create a new financial account (e.g., "HDFC Checking Account") with an initial balance.
- **US-ACC-02:** As a user, I want to view a list of all my active accounts and total net worth summary.
- **US-ACC-03:** As a user, I want to update account metadata (name, type, currency).
- **US-ACC-04:** As a user, I want to soft-delete an account I no longer use.

## Screens
- Accounts Overview Screen (`/accounts`)
- Create Account Modal (`/accounts/new`)
- Edit Account Screen (`/accounts/:id`)

## Navigation Flow
```
[ Dashboard ] ---> Click "Accounts" ---> [ Accounts List Screen ]
                                              |
                          +-------------------+-------------------+
                          |                                       |
                   Click "New Account"                    Click "Edit"
                          v                                       v
               [ Create Account Modal ]                [ Edit Account Screen ]
```

## Database Tables

### `accounts` Table Schema
```sql
CREATE TABLE accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    account_type VARCHAR(50) NOT NULL CHECK (account_type IN ('Checking', 'Savings', 'CreditCard', 'Cash', 'Investment')),
    balance NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_accounts_user_name UNIQUE (user_id, name)
);

CREATE INDEX idx_accounts_user_id ON accounts(user_id);
CREATE INDEX idx_accounts_active ON accounts(user_id, is_active);
```

## Relationships
- `users` (1) <----> (N) `accounts`
- `accounts` (1) <----> (N) `transactions`

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/accounts` | Retrieves all active accounts for the authenticated user | Authorized |
| `GET` | `/api/accounts/{id}` | Gets account details by ID | Authorized |
| `POST` | `/api/accounts` | Creates a new account | Authorized |
| `PUT` | `/api/accounts/{id}` | Updates an existing account | Authorized |
| `DELETE` | `/api/accounts/{id}` | Soft-deletes an account | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Accounts.CreateAccount;

public record CreateAccountRequest(
    string Name,
    string AccountType, // "Checking", "Savings", "CreditCard", "Cash", "Investment"
    decimal InitialBalance,
    string Currency
);
```

```csharp
namespace Fingo.BackendApi.Features.Accounts.UpdateAccount;

public record UpdateAccountRequest(
    string Name,
    string AccountType,
    string Currency
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Accounts.Shared;

public record AccountDto(
    Guid Id,
    string Name,
    string AccountType,
    decimal Balance,
    string Currency,
    bool IsActive,
    DateTime CreatedAt
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Accounts.CreateAccount;

public class CreateAccountValidator : AbstractValidator<CreateAccountRequest>
{
    private static readonly string[] AllowedTypes = ["Checking", "Savings", "CreditCard", "Cash", "Investment"];

    public CreateAccountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters.");

        RuleFor(x => x.AccountType)
            .Must(type => AllowedTypes.Contains(type))
            .WithMessage("Invalid account type. Allowed values: Checking, Savings, CreditCard, Cash, Investment.");

        RuleFor(x => x.Currency)
            .NotEmpty().Length(3).WithMessage("Currency code must be exactly 3 uppercase characters (e.g., USD).");
    }
}
```

## Authorization
- All Account endpoints enforce ownership checks: `WHERE id = @AccountId AND user_id = @UserId`. Accessing accounts owned by other users returns HTTP 404 / HTTP 403.

## Business Logic
1. **Create Account Handler:**
   - Validates uniqueness of `(user_id, name)`.
   - Inserts record into `accounts`.
   - If `InitialBalance > 0`, automatically inserts an initial deposit transaction under systemic "Initial Balance" category.
   - Returns created `AccountDto`.

2. **Delete Account Handler:**
   - Performs update: `UPDATE accounts SET is_active = false, updated_at = NOW() WHERE id = @Id AND user_id = @UserId`.

## Edge Cases
- **Duplicate Account Name:** Unique constraint index throws `PostgresException` state `23505`, caught and returned as HTTP 409 Conflict.

## Error Scenarios
- **Account Not Found:** Returns HTTP 404 with message `"Account not found or access denied."`.

## Future Improvements
- Multi-currency exchange rate recalculations.
- Bank feed auto-sync integrations (Plaid/Yodlee).

## Checklists

### Definition of Done
- [x] Complete vertical slice for Create, Read, Update, Delete accounts.
- [x] Dapper queries optimized with explicit column selections.

### Testing Checklist
- [x] Test duplicate account name returns 409 Conflict.
- [x] Test soft-deleting sets `is_active = false`.

## Dependencies
- Dapper, PostgreSQL, `IDbConnectionFactory`.

## Related Documents
- [Database Architecture](../backend/database.md)
- [Transactions Feature Spec](transactions.md)
