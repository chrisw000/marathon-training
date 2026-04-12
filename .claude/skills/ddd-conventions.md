# DDD Conventions

Applies to: `api/src/MarathonTraining.Domain/` and `api/src/MarathonTraining.Application/`

---

## Aggregate root pattern

Aggregates have:
- Public properties with **private setters** — state is mutated only through the aggregate itself
- A **protected parameterless constructor** for EF Core's proxy/materialization path
- A **public constructor** that takes all required fields — no partial construction
- Mutation methods where behaviour changes internal state (e.g. `StravaConnection.Update(...)`)

**Real example — `AthleteProfile`:**
```csharp
public class AthleteProfile
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public StravaConnection? StravaConnection { get; private set; }

    protected AthleteProfile() { }  // EF Core

    public AthleteProfile(Guid id, string userId, string displayName, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }
}
```

**Real example — `StravaConnection` with mutation method:**
```csharp
public void Update(string accessToken, string refreshToken, DateTimeOffset expiresAt)
{
    AccessToken = accessToken;
    RefreshToken = refreshToken;
    ExpiresAt = expiresAt;
}
```

**Collection aggregates use a private backing field:**
```csharp
private readonly List<Activity> _activities = [];
public IReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();
```

---

## Value object pattern

Value objects are C# `record` types with:
- A **private constructor** — callers cannot `new` them directly
- A **`static Create(...)` factory method** that validates input and throws `DomainException` on failure
- Immutability by record semantics

**Real example — `TssScore`:**
```csharp
public record TssScore
{
    public decimal Value { get; }

    private TssScore(decimal value) { Value = value; }

    public static TssScore Create(decimal value)
    {
        if (value < 0)
            throw new DomainException("TSS score cannot be negative.");
        return new TssScore(value);
    }
}
```

---

## Domain exception

`DomainException` is the only exception type thrown from domain logic. Handlers catch it and
translate to appropriate HTTP responses.

```csharp
// Domain layer
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
```

Usage: throw with a specific, human-readable message. Avoid generic messages like "Error".

---

## Repository interface rules

- Interfaces live in `Domain/Interfaces/` and are named `I{AggregateName}Repository`
- Only one repository interface per aggregate root
- Return types use the aggregate directly (not DTOs)
- All methods are `async Task` with `CancellationToken cancellationToken = default`
- No `IQueryable` — query logic stays inside the repository implementation

**Naming pattern:**
```csharp
Task<T?> GetBy{Criterion}Async(...)
Task AddAsync(T entity, ...)
Task UpdateAsync(T entity, ...)
Task DeleteBy{Criterion}Async(...)
Task UpsertAsync(T entity, ...)
```

---

## Naming conventions

| Thing | Convention | Example |
|---|---|---|
| Aggregate | `class`, PascalCase noun | `AthleteProfile`, `StravaConnection` |
| Value object | `record`, PascalCase noun | `TssScore` |
| Enum | `enum`, PascalCase | `ActivityType` |
| Domain exception | `class DomainException : Exception` | (only one, shared) |
| Repository interface | `I{Name}Repository` in `Domain.Interfaces` | `IAthleteProfileRepository` |
| Command | `record {Name}Command : IRequest` | `ConnectStravaCommand` |
| Query | `record {Name}Query : IRequest<TResult>` | `GetStravaConnectionStatusQuery` |
| Handler | `sealed class {CommandOrQuery}Handler` | `ConnectStravaCommandHandler` |
| Result DTO | `record {Name}Result` or `{Name}Dto` | `EnsureAthleteProfileResult`, `StravaConnectionStatusDto` |

---

## What MUST NOT go in the Domain layer

- EF Core data annotations (`[Key]`, `[Required]`, `[Column]`) — model configuration belongs in `AppDbContext.OnModelCreating`
- MediatR references (`IRequest`, `IRequestHandler`) — these live in Application
- HTTP concerns (`HttpContext`, `IConfiguration`, `ILogger`) — these live in Infrastructure or Api
- `using Microsoft.EntityFrameworkCore` — the Domain project has no EF dependency
- Anything from `System.ComponentModel.DataAnnotations`
- FluentValidation — validators live in Application alongside their commands
