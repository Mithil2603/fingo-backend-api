# Transactions Feature Specification

## Purpose
This document defines the functional behavior, SQL schema, execution workflow, and API contracts for financial transactions (Income, Expense, Account Transfers) in Fingo.

## Business Rules
1. Transactions represent actual movements of money. Types: `Income`, `Expense`, or `Transfer`.
2. Creating an `Income` transaction increases the associated account balance.
3. Creating an `Expense` transaction decreases the associated account balance.
4. Creating a `Transfer` transaction atomically deducts from the source account and credits the destination account.
5. Deleting or updating a transaction automatically adjusts and recalculates the account balances using DB transactions.

## User Stories
- **US-TX-01:** As a user, I want to record an income or expense transaction with date, amount, category, account, and optional note.
- **US-TX-02:** As a user, I want to filter my transactions by date range, account, category, and type with pagination.
- **US-TX-03:** As a user, I want to transfer money between two of my accounts.
- **US-TX-04:** As a user, I want to edit or delete a past transaction and have my account balance update accurately.

## Screens
- Transaction History Ledger (`/transactions`)
- Add Transaction Dialog (`/transactions/new`)
- Account Transfer Dialog (`/transfers/new`)

## Navigation Flow
```
[ Dashboard ] ---> Click "Transactions" ---> [ Transactions Ledger ]
                                                    |
                          +-------------------------+-------------------------+
                          |                                                   |
                 Click "Add Transaction"                             Click "Transfer"
                          v                                                   v
              [ Add Transaction Dialog ]                         [ Transfer Dialog ]
```

## Database Tables

### `transactions` Table Schema
```sql
CREATE TABLE transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    account_id UUID NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES categories(id) ON DELETE RESTRICT,
    amount NUMERIC(14, 2) NOT NULL CHECK (amount > 0),
    transaction_type VARCHAR(20) NOT NULL CHECK (transaction_type IN ('Income', 'Expense', 'Transfer')),
    transaction_date TIMESTAMPTZ NOT NULL,
    description VARCHAR(500),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_transactions_user_date ON transactions(user_id, transaction_date DESC);
CREATE INDEX idx_transactions_account ON transactions(account_id);
CREATE INDEX idx_transactions_category ON transactions(category_id);
```

## Relationships
- `users` (1) <----> (N) `transactions`
- `accounts` (1) <----> (N) `transactions`
- `categories` (1) <----> (N) `transactions`

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/transactions` | Filtered paginated list of transactions | Authorized |
| `GET` | `/api/transactions/{id}` | Gets single transaction details | Authorized |
| `POST` | `/api/transactions` | Creates a new income or expense transaction | Authorized |
| `POST` | `/api/transactions/transfers` | Performs account-to-account transfer | Authorized |
| `PUT` | `/api/transactions/{id}` | Updates transaction details & recalculates balance | Authorized |
| `DELETE` | `/api/transactions/{id}` | Deletes transaction & reverts balance adjustment | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Transactions.CreateTransaction;

public record CreateTransactionRequest(
    Guid AccountId,
    Guid CategoryId,
    decimal Amount,
    string TransactionType, // "Income" or "Expense"
    DateTime TransactionDate,
    string? Description
);
```

```csharp
namespace Fingo.BackendApi.Features.Transactions.CreateTransfer;

public record CreateTransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    DateTime TransferDate,
    string? Description
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Transactions.Shared;

public record TransactionDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Guid CategoryId,
    string CategoryName,
    string CategoryColorHex,
    decimal Amount,
    string TransactionType,
    DateTime TransactionDate,
    string? Description,
    DateTime CreatedAt
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Transactions.CreateTransaction;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Account ID is required.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID is required.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.TransactionType)
            .Must(t => t == "Income" || t == "Expense")
            .WithMessage("Transaction type must be 'Income' or 'Expense'.");
        RuleFor(x => x.TransactionDate).NotEmpty().WithMessage("Transaction date is required.");
    }
}
```

## Authorization
- Transaction endpoints strictly verify ownership of both user ID and related account IDs.

## Business Logic
1. **Create Transaction Handler:**
   - Opens `NpgsqlConnection` and begins `IDbTransaction`.
   - Verifies account ownership and active status.
   - Inserts record into `transactions`.
   - Adjusts balance: `UPDATE accounts SET balance = balance + @Delta WHERE id = @AccountId`.
   - Commits transaction. Returns `TransactionDto`.

2. **Delete Transaction Handler:**
   - Retrieves original transaction record under row lock (`FOR UPDATE`).
   - Calculates reverse balance delta (e.g. if deleted item was Expense of $50, balance increases by +$50).
   - Updates account balance.
   - Marks transaction `is_deleted = true`.
   - Commits transaction.

## Edge Cases
- **Insufficient Funds on Transfer:** Throws `InvalidOperationException("Insufficient balance in source account.")` returning HTTP 400.

## Error Scenarios
- **Account or Category Not Found:** Returns HTTP 404.

## Future Improvements
- Recurring automated transactions (subscriptions, scheduled salary).

## Checklists

### Definition of Done
- [x] Vertical Slice implemented for Create, List, Filter, Transfer, Delete.
- [x] Database transaction rollback verified on failure.

### Testing Checklist
- [x] Test expense creation reduces account balance.
- [x] Test income creation increases account balance.
- [x] Test transfer deducts source and credits destination atomically.

## Dependencies
- Dapper, PostgreSQL, `IDbConnectionFactory`.

## Related Documents
- [Accounts Feature Spec](accounts.md)
- [Categories Feature Spec](categories.md)
