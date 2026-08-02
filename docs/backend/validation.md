# FluentValidation Architecture Specification

## Purpose
This document defines request payload validation using FluentValidation and automatic validation interceptors in ASP.NET Core.

## Scope
Applies to all incoming Request DTOs, endpoint validation pipelines, and validation error formatting.

## Contents

### Validation Principles
1. **Separation of Concerns:** Endpoints and Handlers must never perform manual `if (request == null)` or validation checks. All payload validation occurs before handler execution via FluentValidation validators.
2. **Dedicated Validators:** Every Request DTO must have a matching `AbstractValidator<TRequest>` class located in the same feature slice.
3. **Unified Response Formatting:** Validation failures automatically populate the `Errors` property of the standard `ApiResponse<T>` envelope. Response formatting ownership belongs strictly to [api_response.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/api_response.md).

### Pipeline Interceptor Filter Implementation

```csharp
// File: Infrastructure/Validation/ValidationFilter.cs
using Fingo.BackendApi.Infrastructure.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Fingo.BackendApi.Infrastructure.Validation;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T>? _validator;

    public ValidationFilter(IValidator<T>? validator = null)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_validator == null) return await next(context);

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument == null)
        {
            return Results.BadRequest(ApiResponse<object>.Failure("Request payload cannot be empty."));
        }

        var validationResult = await _validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key.Substring(1),
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return Results.BadRequest(ApiResponse<object>.Failure("Validation failed.", errors));
        }

        return await next(context);
    }
}
```

### Feature Slice Validator Example

```csharp
// File: Features/Transactions/CreateTransaction/CreateTransactionValidator.cs
using FluentValidation;

namespace Fingo.BackendApi.Features.Transactions.CreateTransaction;

public class CreateTransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("Account ID is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Transaction amount must be greater than zero.");

        RuleFor(x => x.TransactionDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Transaction date cannot be in the future.");
    }
}
```

## References
- [Standard API Response Specification](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/api_response.md)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
