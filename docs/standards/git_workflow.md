# Git Workflow, Branching & Commit Conventions Specification

## Purpose
This document defines the Git branching model, branch naming standards, commit message conventions (Conventional Commits), Pull Request (PR) policies, and code review criteria for Fingo repositories.

## Scope
Applies to all human software engineers and AI coding assistants contributing code, tests, or documentation to the Fingo platform.

## Contents

### Branching Model
Fingo uses **Trunk-Based Development** with short-lived feature branches targeting the `main` branch.

```
  main  =========================================================>
          \                             /
  feature  +--- feat/auth-login ------+
```

### Branch Naming Conventions

`{type}/{short-description}`

#### Allowed Branch Types:
- `feat/`: New feature slice or UI screen (e.g., `feat/add-transfer-endpoint`, `feat/accounts-screen`)
- `fix/`: Bug fix (e.g., `fix/jwt-expiration-handling`, `fix/balance-calculation-overflow`)
- `refactor/`: Code reorganization without business logic changes (e.g., `refactor/dapper-query-aliases`)
- `docs/`: Documentation updates (e.g., `docs/update-api-response-spec`)
- `test/`: Unit or integration test additions

### Commit Message Format (Conventional Commits)
Commits MUST follow the [Conventional Commits v1.0.0](https://www.conventionalcommits.org/) specification:

`<type>(<scope>): <short description>`

#### Examples:
- `feat(auth): implement refresh token rotation endpoint`
- `fix(transactions): fix race condition in account balance deduction`
- `docs(backend): update database schema migration guide`
- `test(accounts): add unit tests for account creation validator`

### Pull Request (PR) Requirements
Every Pull Request targeting `main` must meet the following mandatory checklist:
1. **Build & Automated Tests:** All unit and integration tests pass cleanly in CI.
2. **Architecture Compliance:** Code adheres strictly to Vertical Slice / Feature-First Clean Architecture mandates.
3. **No Forbidden Dependencies:** Zero unauthorized NuGet/pub packages added.
4. **Single Responsibility:** PR contains changes limited to a single feature or bugfix scope.

## References
- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [Trunk-Based Development Guide](https://trunkbaseddevelopment.com/)
