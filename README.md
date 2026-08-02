# Fingo Backend Documentation

> Backend documentation for the **Fingo Personal Finance API**.
>
> This documentation defines the architecture, coding standards, development rules, and implementation guidelines for the backend.
>
> Every developer and AI assistant working on this project should use these documents as the single source of truth.

---

# Project Overview

Fingo is a personal finance application that helps users manage:

- Accounts
- Income
- Expenses
- Budgets
- Savings Goals
- Reports
- Financial Insights

The backend is designed using modern enterprise development practices with a focus on maintainability, scalability, and clean architecture.

---

# Technology Stack

| Category           | Technology                              |
| ------------------ | --------------------------------------- |
| Framework          | ASP.NET Core 8 Web API                  |
| Language           | C# 12                                   |
| Database           | PostgreSQL                              |
| Data Access        | Dapper                                  |
| Migrations         | Entity Framework Core (Migrations Only) |
| Authentication     | JWT                                     |
| Validation         | FluentValidation                        |
| API Documentation  | Swagger                                 |
| JSON Serialization | System.Text.Json                        |

---

# Architecture

The backend follows:

- Vertical Slice Architecture
- Modular Monolith
- Feature-Based Organization
- REST API Design
- Dependency Injection
- Dapper for data access

Each feature is isolated into its own module and owns its endpoints, handlers, validation, and database access.

---

# Core Principles

The backend follows these principles:

- Keep features independent.
- Keep endpoints thin.
- Keep business logic inside handlers.
- Keep repositories focused on data access only.
- Prefer explicit SQL over hidden abstractions.
- Write readable code before clever code.
- Prioritize consistency across the codebase.

---

# Documentation Structure (within /docs folder)

## Architecture

| Document                             | Purpose                                            |
| ------------------------------------ | -------------------------------------------------- |
| architecture/backend_architecture.md | Overall backend architecture and design principles |
| architecture/folder_structure.md     | Complete project folder structure                  |
| architecture/project_decisions.md    | Major architectural decisions and their rationale  |

---

## Backend

| Document                        | Purpose                                           |
| ------------------------------- | ------------------------------------------------- |
| backend/database.md             | PostgreSQL design, naming conventions, migrations |
| backend/authentication.md       | JWT authentication and authorization              |
| backend/api_response.md         | Standard API response contracts                   |
| backend/validation.md           | FluentValidation guidelines                       |
| backend/dependency_injection.md | Dependency Injection standards                    |
| backend/error_handling.md       | Global exception handling strategy                |
| backend/logging.md              | Logging standards                                 |

---

## Standards

| Document                        | Purpose                     |
| ------------------------------- | --------------------------- |
| standards/coding_standards.md   | General coding guidelines   |
| standards/naming_conventions.md | Naming conventions          |
| standards/sql_guidelines.md     | SQL writing standards       |
| standards/ai_instructions.md    | Rules for AI-generated code |

---

## Features

Each feature has its own documentation.

Examples:

- features/authentication.md
- features/accounts.md
- features/categories.md
- features/transactions.md
- features/dashboard.md
- features/budgets.md
- features/goals.md
- features/reports.md
- features/settings.md

Feature documentation contains business rules and implementation details specific to that feature.

---

# Development Workflow

Every new feature should follow the same development process.

1. Read the feature documentation.
2. Design the database changes if required.
3. Create Request and Response DTOs.
4. Implement validation.
5. Implement the handler.
6. Implement repository methods.
7. Write SQL queries.
8. Create endpoint.
9. Test the API.
10. Update documentation if architecture changes.

---

# Project Philosophy

This project values:

- Simplicity over complexity
- Consistency over cleverness
- Explicit code over magic
- Maintainability over shortcuts

The objective is to produce code that any experienced .NET developer can understand without needing additional explanation.

---

# Rules for Contributors

Every contributor should:

- Follow the documented architecture.
- Follow naming conventions.
- Keep features self-contained.
- Avoid introducing unnecessary libraries or patterns.
- Update documentation when architectural decisions change.

---

# Rules for AI Assistants

Any AI-generated code must:

- Follow the documented architecture.
- Preserve the existing folder structure.
- Follow coding standards.
- Use Dapper for data access.
- Use FluentValidation for validation.
- Keep handlers responsible for business logic.
- Keep repositories responsible only for database operations.
- Avoid introducing new architectural patterns unless explicitly requested.

---

# Versioning

Documentation should evolve alongside the project.

Whenever an architectural decision changes:

- Update the relevant document.
- Record the change in `CHANGELOG.md`.
- Keep the documentation synchronized with the codebase.

---

# Scope

This documentation covers the backend only.

Flutter documentation is maintained separately and follows its own architecture and standards.
