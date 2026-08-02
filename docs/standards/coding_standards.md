# C# 12 & .NET 8 Coding Standards Specification

## Purpose
This document defines C# 12 language rules, SOLID principles, async/await best practices, nullable reference type handling, and file formatting standards for `fingo-backend-api`.

## Scope
Applies to all C# source code files, feature slices, middleware, and infrastructure classes in the backend solution.

## Contents

### Language Feature Standards (C# 12)
1. **Primary Constructors:** Use primary constructors for class and record dependency injection to remove verbose private field assignments.
   ```csharp
   // GOOD: Primary Constructor DI
   public class CreateAccountHandler(IDbConnectionFactory factory, ILogger<CreateAccountHandler> logger)
   {
       // Use factory and logger directly
   }
   ```
2. **Records for DTOs:** All Request and Response DTOs MUST be immutable `record` types.
   ```csharp
   public record CreateAccountRequest(string Name, decimal InitialBalance, string AccountType);
   ```
3. **Collection Expressions:** Prefer C# 12 collection expressions (`[]`) over `new List<T>()` or `Array.Empty<T>()`.
4. **Nullable Reference Types:** `#nullable enable` is mandatory across all projects. No warnings allowed.

### Component Responsibilities & Data Access Execution
All data access execution rules belong strictly to [backend_architecture.md](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/architecture/backend_architecture.md).
- Handlers execute Dapper queries directly via `IDbConnectionFactory`.
- Do NOT introduce generic repositories, unit of work classes, or pass-through repository interfaces.

### Async / Await Standards
- Always append `Async` suffix to asynchronous methods (`HandleAsync`, `ExecuteAsync`).
- Always pass `CancellationToken` through to Dapper `CommandDefinition`.
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` which cause threadpool starvation.

## References
- [Backend Architecture Specification](file:///c:/Users/mithi/OneDrive/Documents/Full%20Stack%20Projects/Fingo/fingo-backend-api/docs/architecture/backend_architecture.md)
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
