# Authentication & Identity Feature Specification

## Purpose
This document provides the complete technical specification for user registration, authentication, token refresh, password recovery, and identity verification in Fingo.

## Business Rules
1. Every user must have a unique email address.
2. Passwords must be at least 8 characters long, containing at least one uppercase letter, one lowercase letter, one numeric digit, and one special character.
3. Access tokens expire after 60 minutes; refresh tokens expire after 30 days.
4. Revoked or expired refresh tokens cannot be used to generate new access tokens.
5. Failed login attempts do not reveal whether the email or password was invalid (prevents user enumeration).

## User Stories
- **US-AUTH-01:** As a new user, I want to create an account with my email and password so that I can start tracking my finances.
- **US-AUTH-02:** As a registered user, I want to log in securely with my credentials to receive access tokens.
- **US-AUTH-03:** As an authenticated user, I want my token to refresh automatically without interrupting my session.
- **US-AUTH-04:** As a user, I want to log out so that my active refresh tokens are invalidated.

## Screens
- Register Screen (`/register`)
- Login Screen (`/login`)
- Forgot Password Screen (`/forgot-password`)
- Reset Password Screen (`/reset-password`)

## Navigation Flow
```
[ App Launch ] ---> Check Valid JWT Token
                         |
           +-------------+-------------+
           |                           |
      (Token Valid)             (Token Missing / Expired)
           |                           |
    v                       v
[ Dashboard Screen ]        [ Login Screen ] <---> [ Register Screen ]
```

## Database Tables

### 1. `users` Table Schema
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(256) NOT NULL UNIQUE,
    password_hash VARCHAR(500) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    is_email_verified BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX idx_users_email ON users(email);
```

### 2. `refresh_tokens` Table Schema
```sql
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(500) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
```

## Relationships
- `users` (1) <----> (N) `refresh_tokens` (One user can have multiple refresh tokens across devices).

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Registers a new user account | Anonymous |
| `POST` | `/api/auth/login` | Authenticates credentials and returns tokens | Anonymous |
| `POST` | `/api/auth/refresh` | Generates a new access token using refresh token | Anonymous |
| `POST` | `/api/auth/logout` | Revokes the specified refresh token | Authorized |
| `GET` | `/api/auth/me` | Returns current user profile details | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Authentication.Register;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
```

```csharp
namespace Fingo.BackendApi.Features.Authentication.Login;

public record LoginRequest(
    string Email,
    string Password
);
```

```csharp
namespace Fingo.BackendApi.Features.Authentication.RefreshToken;

public record RefreshTokenRequest(
    string RefreshToken
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Authentication.Login;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileDto User
);

public record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsEmailVerified
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Authentication.Register;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");
    }
}
```

## Authorization
- Endpoints `/api/auth/register`, `/api/auth/login`, and `/api/auth/refresh` permit anonymous access (`[AllowAnonymous]`).
- `/api/auth/me` and `/api/auth/logout` enforce `[Authorize]` attributes and extract the `ClaimTypes.NameIdentifier` claim.

## Business Logic
1. **Registration Handler:**
   - Normalizes email to lowercase.
   - Checks database for existing email (`SELECT COUNT(1) FROM users WHERE email = @Email`).
   - Hashes raw password using `IPasswordHasherService`.
   - Inserts record into `users` table via Dapper.
   - Automatically provisions default user categories (Income/Expense defaults).
   - Generates JWT Access Token and Refresh Token.
   - Saves Refresh Token to database.
   - Returns `AuthResponse`.

2. **Login Handler:**
   - Queries user by normalized email.
   - Verifies password hash using `IPasswordHasherService.VerifyPassword()`.
   - If invalid, throws `InvalidOperationException("Invalid credentials.")`.
   - Generates new token pair, persists refresh token, and returns `AuthResponse`.

## Edge Cases
- **Concurrent Logins:** Supported (multiple active refresh tokens permitted per user).
- **Stale Tokens:** Background cleaner job or query filter purges expired refresh tokens (`expires_at < NOW()`).

## Error Scenarios
- **Duplicate Email:** Returns HTTP 409 Conflict with `ApiResponse<object>.Failure("Email address is already registered.")`.
- **Invalid Password:** Returns HTTP 400 Bad Request with generic "Invalid email or password" message.

## Future Improvements
- Multi-Factor Authentication (MFA / TOTP).
- OAuth2 Social Logins (Google, Apple Sign-In).

## Checklists

### Definition of Done
- [x] Full Vertical Slice implemented (`Endpoint.cs`, `Handler.cs`, `Request.cs`, `Response.cs`, `Validator.cs`).
- [x] Unit tests written for Register & Login handlers.
- [x] Swagger documentation verified for Auth endpoints.

### Testing Checklist
- [x] Verify registration with valid details returns HTTP 201 Created and JWT.
- [x] Verify duplicate email registration returns HTTP 409 Conflict.
- [x] Verify login with wrong password returns HTTP 400 Bad Request.

## Dependencies
- Dapper, Npgsql, `IPasswordHasherService`, `IJwtProvider`.

## Related Documents
- [Authentication Infrastructure](../backend/authentication.md)
- [API Response Envelope](../backend/api_response.md)
