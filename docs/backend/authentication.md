# Authentication & Security Specification

## Purpose
This document specifies the authentication, authorization, token lifecycle, password security, and identity infrastructure for the Fingo backend API.

## Scope
Covers JWT access token generation, refresh token management, ASP.NET Core Authentication scheme configuration, password hashing using `IPasswordHasher<TUser>`, claims extraction, and HTTP security headers.

## Contents

### JWT Architecture & Token Lifecycle
Fingo uses stateless **JSON Web Tokens (JWT)** for securing API endpoints.

- **Access Token:** Short-lived JWT (15-60 minute expiration) containing `sub` (User ID), `email`, and standard claims. Signed using HMAC-SHA256 with a secret key.
- **Refresh Token:** Cryptographically strong random token stored securely in PostgreSQL with an expiration window (7 to 30 days). Used to acquire new access tokens without requiring re-entering credentials.

```
+-----------------------------------------------------------------------------------+
|                            JWT AUTHENTICATION FLOW                                |
|                                                                                   |
|  +--------------+  1. POST /api/auth/login (Email/Password)   +----------------+  |
|  |              |-------------------------------------------->|                |  |
|  |  Client App  |  2. Returns JWT Access Token + Refresh Token| Fingo Auth API |  |
|  |              |<--------------------------------------------|                |  |
|  +--------------+                                             +----------------+  |
|         |                                                                         |
|         | 3. Subsequent Requests: Header "Authorization: Bearer <JWT>"            |
|         +------------------------------------------------------------------------>|
+-----------------------------------------------------------------------------------+
```

### Claims Standard
The JWT payload embeds standard claims:
- `ClaimTypes.NameIdentifier` (`sub`): User's unique `Guid`.
- `ClaimTypes.Email` (`email`): User's primary email address.
- `JwtRegisteredClaimNames.Jti`: Unique token identification GUID.

### Password Security Strategy
- Passwords MUST never be stored in plaintext.
- Passwords are hashed using ASP.NET Core's built-in `PasswordHasher<UserIdentity>` (PBKDF2 with HMAC-SHA512 or Argon2 depending on .NET framework defaults).
- Custom hashing algorithms are strictly prohibited.

## Best Practices
- Never transmit sensitive user claims (like passwords or refresh tokens) inside the JWT payload.
- Always validate `Issuer`, `Audience`, `Lifetime`, and `IssuerSigningKey` during JWT verification.
- Always check that refresh tokens are not revoked or expired prior to issuing a new access token.

## Concrete Examples

### 1. JWT Token Service Provider (`IJwtProvider`)

```csharp
// File: Infrastructure/Authentication/IJwtProvider.cs
namespace Fingo.BackendApi.Infrastructure.Authentication;

public interface IJwtProvider
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
}
```

```csharp
// File: Infrastructure/Authentication/JwtProvider.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Fingo.BackendApi.Infrastructure.Authentication;

public class JwtProvider : IJwtProvider
{
    private readonly IConfiguration _configuration;

    public JwtProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Guid userId, string email)
    {
        var secretKey = _configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("Jwt:SecretKey configuration is missing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
```

### 2. Password Hashing Utility Integration

```csharp
// File: Infrastructure/Authentication/IPasswordHasherService.cs
namespace Fingo.BackendApi.Infrastructure.Authentication;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
```

```csharp
// File: Infrastructure/Authentication/PasswordHasherService.cs
using Microsoft.AspNetCore.Identity;

namespace Fingo.BackendApi.Infrastructure.Authentication;

public class UserDummyIdentity { }

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<UserDummyIdentity> _hasher = new();
    private readonly UserDummyIdentity _userContext = new();

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(_userContext, password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword(_userContext, passwordHash, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
```

### 3. Service Registration Extension

```csharp
// File: Infrastructure/Authentication/AuthenticationSetup.cs
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Fingo.BackendApi.Infrastructure.Authentication;

public static class AuthenticationSetup
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("Jwt:SecretKey is required.");

        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }
}
```

## References
- RFC 7519: JSON Web Token (JWT) Specification
- NIST SP 800-63B: Digital Identity Guidelines
- ASP.NET Core PasswordHasher Technical Specification

## Notes
- Jwt SecretKey must be kept secret and configured via environment variables in production.
