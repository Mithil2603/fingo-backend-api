# Architectural Decision Records (ADRs) & Trade-offs

## Purpose
This document records key architectural decisions, rationale, trade-offs, and technology selections made during the engineering design of the Fingo backend platform.

## Scope
Applies to framework choices, ORM selection, data storage choices, messaging & handler invocation patterns, and strict technical bans.

## Contents

### Key Architectural Decision Records (ADRs)

#### ADR-001: Selection of Vertical Slice Architecture over Layered Architecture
- **Status:** Accepted
- **Context:** Layered Architecture forces developer code changes across controllers, application services, domain models, generic repositories, and database contexts for every minor feature change.
- **Decision:** Adopt **Vertical Slice Architecture**, organizing code by business features rather than technical layers.
- **Consequences:** Code co-location dramatically increases productivity. Slices can be altered, optimized, or deleted without risking regressions in unrelated feature slices.

#### ADR-002: Dapper for All Runtime Data Operations & EF Core for Migrations ONLY
- **Status:** Accepted
- **Context:** Full ORMs (like Entity Framework Core) introduce tracking overhead, complex LINQ-to-SQL translation bugs, unexpected N+1 query execution, and high memory allocation.
- **Decision:** Use **Dapper** micro-ORM for 100% of runtime data queries and commands. EF Core `DbContext` is used *exclusively* for managing schema definitions and running CLI migrations (`dotnet ef migrations add`).
- **Consequences:** Near C-level SQL performance, explicit SQL query optimization, transparent query debugging, and complete control over PostgreSQL indexing capabilities.

#### ADR-003: Rejection of MediatR in Favor of Explicit Dependency Injection Handlers
- **Status:** Accepted
- **Context:** MediatR introduces runtime reflection lookups, indirect control flow, opaque stack trace paths, and obfuscated IDE code navigation ("Find Implementations" becomes fuzzy).
- **Decision:** Inject feature-specific `Handler` classes directly into `Endpoint` controllers via ASP.NET Core built-in DI (`builder.Services.AddScoped<CreateTransactionHandler>()`).
- **Consequences:** Zero-overhead invocation, instant F12 navigation in IDEs, clean static analysis, and deterministic dependency resolution.

#### ADR-004: FluentValidation Middleware Filter Pipeline over Attribute Validation
- **Status:** Accepted
- **Context:** Inline DataAnnotation attributes (`[Required]`, `[StringLength]`) mix validation metadata into request DTO declarations and lack complex validation capabilities (e.g., cross-field comparison).
- **Decision:** Standardize on **FluentValidation** with separate `AbstractValidator<T>` rules classes bound to ASP.NET Core request pipeline.
- **Consequences:** Clean DTO definitions, reusable rule sets, rich validation capability (regex, domain rules, comparison), and centralized validation error formatting.

### Forbidden Technologies & Anti-Patterns Matrix

| Forbidden Technology / Pattern | Reason for Prohibition | Mandatory Replacement |
| :--- | :--- | :--- |
| **EF Core CRUD Queries** | Performance overhead, implicit LINQ generation, N+1 query risks. | Standardized Dapper SQL execution via `IDbConnectionFactory`. |
| **MediatR / In-Memory Buses** | Obfuscated navigation, reflection startup overhead, unnecessary indirection. | Strongly typed feature `Handler` classes registered in DI. |
| **AutoMapper** | Magic object-to-object mapping, hidden exceptions, runtime performance penalty. | Explicit constructor mapping or C# 12 positional record instantiation. |
| **Generic Repositories (`IRepository<T>`)** | Leaky abstraction, forces memory-based filtering, masks SQL capabilities. | Direct Dapper SQL queries tuned specifically for each feature handler. |
| **Unit of Work Pattern (`IUnitOfWork`)** | Duplicate abstraction over `IDbTransaction` / `IDbConnection`. | Native `IDbTransaction` scoped cleanly within Handler execution blocks. |
| **Newtonsoft.Json** | Heavy memory usage, legacy package dependency. | Native high-performance `System.Text.Json`. |
| **Massive Controllers** | Violates Single Responsibility Principle, leads to giant dumping-ground files. | Isolated single-action `Endpoint.cs` controller per slice. |
| **Business Logic in Controllers/Repos** | Breaks testability and co-location principles. | All business rules encapsulated inside feature `Handler.cs`. |

## Best Practices
- Every new developer must review these ADRs before proposing architectural alterations.
- Any decision to add a new package or framework must pass an ADR review evaluating performance, complexity, and alignment with Vertical Slice principles.

## Concrete Examples

### Comparison: Forbidden Pattern vs. Fingo Standard Pattern

#### ❌ Forbidden Approach (Layered + AutoMapper + MediatR + Generic Repository)
```csharp
// FORBIDDEN: Do NOT write code like this in Fingo
public class TransactionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    public TransactionController(IMediator mediator, IMapper mapper) 
    { 
        _mediator = mediator; 
        _mapper = mapper; 
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        var command = _mapper.Map<CreateTransactionCommand>(dto);
        var result = await _mediator.Send(command); // Indirection hides execution path
        return Ok(result);
    }
}
```

#### ✔ Fingo Standard Approach (Vertical Slice + Dapper + Direct Handler)
```csharp
// FINGO STANDARD: Direct, explicit, high-performance execution
namespace Fingo.BackendApi.Features.Transactions.CreateTransaction;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class CreateTransactionEndpoint : ControllerBase
{
    private readonly CreateTransactionHandler _handler;

    public CreateTransactionEndpoint(CreateTransactionHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _handler.HandleAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, ApiResponse<CreateTransactionResponse>.SuccessResponse(result));
    }
}
```

## References
- Architectural Decision Records Guide (Joel Parker Henderson)
- Performance Comparison: Dapper vs Entity Framework Core
- Software Architecture Patterns by Mark Richards

## Notes
- ADR status can only be modified through explicit engineering RFC review.
