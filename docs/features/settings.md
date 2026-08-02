# User Profile & Settings Feature Specification

## Purpose
This document defines user profile management, password updates, currency preferences, account deletion, and application settings in Fingo.

## Business Rules
1. Users can update their first name, last name, default primary currency, and dark mode preference.
2. Changing password requires verifying the existing password hash first.
3. User profile details cannot be modified by unauthenticated users.
4. Account deletion wipes or anonymizes all associated financial data under strict GDPR compliance.

## User Stories
- **US-SET-01:** As a user, I want to edit my personal profile (name, currency preference).
- **US-SET-02:** As a user, I want to change my account password securely.
- **US-SET-03:** As a user, I want to delete my account and purge my personal data.

## Screens
- Profile & Settings Screen (`/settings`)
- Security & Password Modal (`/settings/security`)

## Navigation Flow
```
[ Top Bar / Avatar ] ---> Click "Settings" ---> [ User Settings View ]
                                                       |
                          +----------------------------+----------------------------+
                          |                                                         |
                 Click "Edit Profile"                                     Click "Security"
                          v                                                         v
                [ Update Profile Form ]                                  [ Change Password Form ]
```

## Database Tables

### `user_settings` Table Schema
```sql
CREATE TABLE user_settings (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    primary_currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    theme_preference VARCHAR(20) NOT NULL DEFAULT 'System' CHECK (theme_preference IN ('Light', 'Dark', 'System')),
    email_notifications_enabled BOOLEAN NOT NULL DEFAULT true,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Relationships
- `users` (1) <----> (1) `user_settings`

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/settings` | Gets current user settings & profile details | Authorized |
| `PUT` | `/api/settings/profile` | Updates user profile name & currency preferences | Authorized |
| `PUT` | `/api/settings/password` | Changes account password | Authorized |
| `DELETE` | `/api/settings/account` | Permanently deletes account and data | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Settings.UpdateProfile;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string PrimaryCurrency,
    string ThemePreference
);
```

```csharp
namespace Fingo.BackendApi.Features.Settings.ChangePassword;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Settings.GetSettings;

public record UserSettingsResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string PrimaryCurrency,
    string ThemePreference,
    bool EmailNotificationsEnabled
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Settings.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.");
    }
}
```

## Authorization
- Endpoints require valid JWT authorization (`[Authorize]`). Operations target `@UserId` extracted from claim context.

## Business Logic
1. **Change Password Handler:**
   - Retrieves user record from database by `@UserId`.
   - Verifies `CurrentPassword` matches hash via `IPasswordHasherService.VerifyPassword()`.
   - If invalid, throws `InvalidOperationException("Current password is incorrect.")`.
   - Hashes `NewPassword` and updates database record.
   - Revokes all active refresh tokens for the user (`UPDATE refresh_tokens SET is_revoked = true WHERE user_id = @UserId`).

## Edge Cases
- **Reusing Old Password:** Allowed or blocked based on security settings.

## Error Scenarios
- **Incorrect Current Password:** Returns HTTP 400 Bad Request.

## Future Improvements
- Multi-language localization settings (i18n).

## Checklists

### Definition of Done
- [x] Profile, password update, and account deletion endpoints implemented.
- [x] Refresh token revocation upon password change verified.

### Testing Checklist
- [x] Verify changing password invalidates existing refresh tokens.

## Dependencies
- Dapper, PostgreSQL, `IPasswordHasherService`, `IDbConnectionFactory`.

## Related Documents
- [Authentication Feature Spec](authentication.md)
- [Database Architecture](../backend/database.md)
