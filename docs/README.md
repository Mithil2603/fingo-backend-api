# Fingo Backend API Documentation

## Purpose
This document serves as the master index and central navigation hub for the entire Fingo backend documentation suite. Fingo is a personal finance management platform engineered with ASP.NET Core 8 Web API, PostgreSQL, Dapper, and a Vertical Slice Modular Monolith architecture.

## Scope
This documentation suite covers all architectural patterns, engineering standards, infrastructure setup, backend system components, and feature domain specifications required to build, test, deploy, and maintain the Fingo backend system.

## Navigation Index

### Architecture Specifications
1. [Backend Architecture](architecture/backend_architecture.md) - Deep dive into Vertical Slice Architecture, Modular Monolith design, CQRS execution flow without MediatR, and Handler data access rules.
2. [Folder Structure](architecture/folder_structure.md) - Folder layout, file conventions, feature isolation rules, and spatial organization.
3. [Project Decisions](architecture/project_decisions.md) - **Single Canonical Owner** of technology selection, ADRs, trade-offs, rationale, and forbidden technologies.

### Core Backend Infrastructure
4. [Database Architecture & Dapper](backend/database.md) - Connection management, Npgsql, Dapper mapping, transactions, EF Core migration commands.
5. [Authentication & Security](backend/authentication.md) - JWT token issuance, refresh tokens, password hashing with `IPasswordHasher<T>`.
6. [Standard API Response](backend/api_response.md) - **Single Canonical Owner** of API response envelope design `ApiResponse<T>`, pagination metadata, error formats.
7. [Request Validation](backend/validation.md) - FluentValidation pipeline, automatic validation filters, custom validator examples.
8. [Dependency Injection](backend/dependency_injection.md) - Extension methods, service lifetimes (Scoped, Singleton, Transient), clean `Program.cs`.
9. [Error Handling & Middleware](backend/error_handling.md) - Centralized exception handling middleware, HTTP status codes, ProblemDetails formatting.
10. [Structured Logging](backend/logging.md) - `ILogger<T>` implementation, log correlation IDs, structured parameter logging.
11. [Caching Architecture](backend/caching.md) - Cache-aside pattern, Redis / In-Memory providers, TTL policies, invalidation triggers.
12. [Rate Limiting & API Security](backend/rate_limiting_security.md) - Sliding window rate limits, auth endpoint brute-force protection, CORS rules, security headers.

### Engineering Standards & Guidelines
13. [Coding Standards](standards/coding_standards.md) - C# 12 conventions, immutability, async/await rules, nullable reference handling.
14. [Naming Conventions](standards/naming_conventions.md) - PascalCase, camelCase, snake_case, route kebab-casing rules.
15. [SQL Guidelines](standards/sql_guidelines.md) - PostgreSQL SQL formatting, indexing strategies, parameterized query rules.
16. [Git Workflow & Branching](standards/git_workflow.md) - Trunk-Based Development, Conventional Commits, Pull Request checklists.
17. [AI Engineering Instructions](standards/ai_instructions.md) - Strict guidelines for AI code generation models working on Fingo.

### Setup & Environment Guides
18. [Getting Started](setup/getting_started.md) - Step-by-step developer onboarding, clone to run walkthrough.
19. [Required Packages](setup/required_packages.md) - Explicit dependency inventory and version management rules.
20. [Environment Setup](setup/environment_setup.md) - Environment variables, `appsettings.json`, Docker compose setups.
21. [CI/CD Pipeline & Deployment](setup/cicd_deployment.md) - Docker build specs, GitHub Actions workflow, production migration deployment.

### Feature Specifications
22. [Authentication Feature](features/authentication.md) - Login, register, refresh token, password reset, profile slices.
23. [Accounts Feature](features/accounts.md) - Bank accounts, wallets, credit cards management, balance recalculation.
24. [Categories Feature](features/categories.md) - Income and expense categories, system defaults, user custom categories.
25. [Transactions Feature](features/transactions.md) - Financial transaction creation, filtering, transfer handling, history.
26. [Dashboard Feature](features/dashboard.md) - Financial summary metrics, recent activity, spending breakdowns.
27. [Budgets Feature](features/budgets.md) - Category budget targets, period tracking, threshold alerts.
28. [Goals Feature](features/goals.md) - Savings goals, target dates, progress tracking, contribution logs.
29. [Reports Feature](features/reports.md) - Cash flow analysis, net worth progression, monthly expense reports.
30. [Settings Feature](features/settings.md) - User profile, currency preferences, dark theme, export capabilities.

## Best Practices
- Read the relevant feature specification before creating or altering code within a feature slice.
- Respect domain boundaries: feature slices should not reference other feature slices' internal classes directly.
- Never bypass the FluentValidation pipeline by manual inline controller validation.
- All database queries must be strictly parameterized to prevent SQL injection vulnerabilities.

## References
- [Vertical Slice Architecture by Jimmy Bogard](https://jimmybogard.com/vertical-slice-architecture/)
- [Microsoft ASP.NET Core 8 Web API Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Dapper Micro-ORM Tutorial](https://github.com/DapperLib/Dapper)
