# Categories Feature Specification

## Purpose
This document defines the functional and technical specification for category management (Income and Expense categories, hierarchical subcategories, custom hex colors, and icons) in Fingo.

## Business Rules
1. Categories are partitioned by `type`: `Income` or `Expense`.
2. System categories (`is_system = true`) are global defaults provided to all users and cannot be modified or deleted.
3. User categories (`is_system = false`) are private to the owning user.
4. Categories support one level of optional subcategory hierarchy (`parent_id`).
5. A category cannot be deleted if active transactions are assigned to it.

## User Stories
- **US-CAT-01:** As a user, I want to see default categories (e.g., Food, Housing, Salary, Utilities).
- **US-CAT-02:** As a user, I want to create custom expense/income categories with custom icons and color hex codes.
- **US-CAT-03:** As a user, I want to view my category hierarchy when assigning transactions.

## Screens
- Category Management Screen (`/categories`)
- Add/Edit Category Modal (`/categories/modal`)

## Navigation Flow
```
[ Settings / Nav ] ---> Click "Categories" ---> [ Categories Tab View ]
                                                       |
                                            (Filter by Income / Expense)
                                                       v
                                            [ Category Tree View ]
```

## Database Tables

### `categories` Table Schema
```sql
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE, -- NULL for system categories
    parent_id UUID REFERENCES categories(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    category_type VARCHAR(20) NOT NULL CHECK (category_type IN ('Income', 'Expense')),
    icon_name VARCHAR(50) NOT NULL DEFAULT 'folder',
    color_hex VARCHAR(7) NOT NULL DEFAULT '#6c757d',
    is_system BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_categories_user_type ON categories(user_id, category_type);
```

## Relationships
- `users` (1) <----> (N) `categories`
- `categories` (Self 1:N) `categories` (`parent_id`)
- `categories` (1) <----> (N) `transactions`

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/categories` | Gets list of all categories (System + User Custom) | Authorized |
| `POST` | `/api/categories` | Creates a custom user category | Authorized |
| `PUT` | `/api/categories/{id}` | Updates a user custom category | Authorized |
| `DELETE` | `/api/categories/{id}` | Deletes a custom user category if unused | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Categories.CreateCategory;

public record CreateCategoryRequest(
    string Name,
    string CategoryType, // "Income" or "Expense"
    Guid? ParentId,
    string IconName,
    string ColorHex
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Categories.Shared;

public record CategoryDto(
    Guid Id,
    string Name,
    string CategoryType,
    Guid? ParentId,
    string IconName,
    string ColorHex,
    bool IsSystem,
    List<CategoryDto>? Subcategories
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Categories.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.CategoryType)
            .Must(type => type == "Income" || type == "Expense")
            .WithMessage("Category type must be either 'Income' or 'Expense'.");

        RuleFor(x => x.ColorHex)
            .Matches(@"^#(?:[0-9a-fA-F]{3}){1,2}$")
            .WithMessage("ColorHex must be a valid hex color code (e.g., #FF5733).");
    }
}
```

## Authorization
- System categories are read-only for all users.
- Custom categories enforce `WHERE id = @Id AND user_id = @UserId AND is_system = false`.

## Business Logic
1. **Get Categories Handler:**
   - Queries `SELECT * FROM categories WHERE user_id = @UserId OR is_system = true ORDER BY name ASC`.
   - Constructs parent-child tree hierarchy in memory and returns `List<CategoryDto>`.

2. **Delete Category Handler:**
   - Checks if transactions reference the category: `SELECT COUNT(1) FROM transactions WHERE category_id = @Id`.
   - If count > 0, throws `InvalidOperationException("Cannot delete category with associated transactions.")`.
   - Deletes category record via Dapper.

## Edge Cases
- **Deleting System Category:** Forbidden via SQL predicate `AND is_system = false`. Throws 400 Bad Request.

## Error Scenarios
- **Category In Use:** Returns HTTP 400 Bad Request with error list containing usage details.

## Future Improvements
- AI-based auto-categorization based on transaction merchant descriptions.

## Checklists

### Definition of Done
- [x] Full vertical slice for category queries and tree construction.
- [x] Pre-seeded system defaults verified.

### Testing Checklist
- [x] Verify system categories cannot be deleted.
- [x] Verify subcategories correctly attach to parent nodes.

## Dependencies
- Dapper, PostgreSQL, `IDbConnectionFactory`.

## Related Documents
- [Transactions Feature Spec](transactions.md)
- [Budgets Feature Spec](budgets.md)
