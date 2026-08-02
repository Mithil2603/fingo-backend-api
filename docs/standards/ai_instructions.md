# AI Coding Assistant Instructions & Mandatory Verification Checklist (Backend)

## Purpose
This document establishes non-negotiable rules, architecture constraints, file structure rules, and pre-generation verification checklists for AI coding assistants (Claude, ChatGPT, Gemini, Copilot) working on `fingo-backend-api`.

## Scope
Applies to every automated code generation, refactoring, feature implementation, and bugfix prompt processed for the backend.

## Contents

### Non-Negotiable AI Rules (Banned Technologies & Patterns)
AI coding assistants **MUST NEVER** generate code using:
- ❌ Entity Framework Core for CRUD queries (`.Add()`, `.Update()`, `.SaveChanges()`, `.ToList()`). (EF is allowed ONLY for `dotnet ef migrations`).
- ❌ MediatR or custom mediator patterns.
- ❌ AutoMapper, Mapster, or third-party object mapping libraries.
- ❌ Generic Repository or Unit of Work classes.
- ❌ Pass-through Repository layers for simple feature slices.
- ❌ Newtonsoft.Json. Use strictly `System.Text.Json`.
- ❌ Controllers with multiple action methods. Every operation MUST be an isolated Vertical Slice (`Endpoint.cs`, `Handler.cs`, `Request.cs`, `Response.cs`, `Validator.cs`).

### Single Source of Truth References
AI assistants MUST consult canonical owner documents for specific domain rules:
- **Architecture & Component Responsibilities:** [backend_architecture.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/architecture/backend_architecture.md)
- **Database & Dapper Execution:** [database.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/database.md)
- **API Response & Error Envelopes:** [api_response.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/api_response.md)
- **Validation Rules:** [validation.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/validation.md)
- **Caching Rules:** [caching.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/caching.md)
- **Rate Limiting & Security:** [rate_limiting_security.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/backend/rate_limiting_security.md)

### AI Pre-Generation Checklist
Before returning code, the AI tool MUST verify:
1. [ ] Did I create an isolated vertical slice inside `Features/{Domain}/{Operation}/`?
2. [ ] Does `Handler.cs` execute Dapper queries directly via `IDbConnectionFactory`?
3. [ ] Are all SQL column names mapped to C# properties using PascalCase aliases (`SELECT user_id AS UserId`)?
4. [ ] Does the endpoint return `ApiResponse<T>` matching the canonical dictionary error envelope when validation/exceptions occur?
5. [ ] Is every parameter in Dapper queries parameterized (`@UserId`, `@Amount`) to prevent SQL injection?

## References
- [Project Decisions ADR](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/architecture/project_decisions.md)
