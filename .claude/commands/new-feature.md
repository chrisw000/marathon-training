# /new-feature — Add a domain feature end to end

**Usage:** `/new-feature <FeatureName>`  
Example: `/new-feature TrainingWeekSummary`

---

Before writing any code, read these three skill documents in full:
- `.claude/skills/ddd-conventions.md`
- `.claude/skills/testing-patterns.md`
- `.claude/skills/api-endpoints.md`

Then carry out every step below in order. Do not skip steps. Do not commit until step 8 passes.

---

## Step 1 — Domain layer

In `api/src/MarathonTraining.Domain/`:

1. Create or extend the aggregate in `Aggregates/{Name}.cs` following the pattern in
   `ddd-conventions.md` — private setters, protected EF constructor, public constructor.
2. If the feature introduces a value object, add it to `ValueObjects/{Name}.cs` with a
   private constructor and `static Create(...)` factory that throws `DomainException` on
   invalid input.
3. Add a repository interface to `Interfaces/I{Name}Repository.cs` if a new aggregate is
   introduced. Follow the method naming convention in `ddd-conventions.md`.
4. Do not add EF attributes, MediatR references, or any external package dependency
   to the Domain project.

---

## Step 2 — Application layer

In `api/src/MarathonTraining.Application/{FeatureName}/`:

1. Create a command record: `{Action}{FeatureName}Command.cs`
   ```csharp
   public sealed record {Action}{FeatureName}Command(...) : IRequest<{Name}Result>;
   public sealed record {Name}Result(...);
   ```
2. Create the command handler: `{Action}{FeatureName}CommandHandler.cs`
   — `sealed class`, primary-constructor DI, implement `IRequestHandler<,>`.
3. If the feature has a read path, create a query: `Get{FeatureName}Query.cs`
   and `Get{FeatureName}QueryHandler.cs` with a `{Name}Dto` result record.
4. Handlers must only depend on domain interfaces (repositories) and application-layer
   service interfaces. No direct DbContext or HttpClient references.

---

## Step 3 — Infrastructure layer

In `api/src/MarathonTraining.Infrastructure/`:

1. If a new repository interface was defined in step 1, implement it in
   `Persistence/Repositories/{Name}Repository.cs`.
2. Register the implementation in the DI step (step 4) — do not use `[Service]` attributes.
3. If the feature needs a new DbSet, add it to `AppDbContext` and configure it in
   `OnModelCreating`. Follow index conventions in `ef-migrations.md`.
4. If schema changes are needed, note them — migration generation is a separate step
   (`/new-migration`).

---

## Step 4 — DI registration

In `api/src/MarathonTraining.Api/Program.cs`:

```csharp
// Repositories
builder.Services.AddScoped<I{Name}Repository, {Name}Repository>();
```

MediatR handlers are discovered automatically via the assembly scan of
`typeof(ConnectStravaCommand).Assembly` — no explicit handler registration needed.

---

## Step 5 — API endpoint(s)

In `Program.cs`, add one or more minimal API endpoints following the patterns in
`api-endpoints.md`:

```csharp
app.MapPost("/api/{resource}", async (ClaimsPrincipal user, ISender sender) =>
{
    var userId = user.GetObjectId()
        ?? throw new InvalidOperationException("No object ID claim found.");
    var result = await sender.Send(new {Command}(userId, ...));
    return Results.Ok(result);
})
.WithName("{EndpointName}")
.WithTags("{FeatureTag}");
```

Add the new endpoint(s) to the reference table in `.claude/skills/api-endpoints.md`.

---

## Step 6 — Unit tests

In `api/tests/MarathonTraining.Application.Tests/{FeatureName}/`:

1. Create `{HandlerName}Tests.cs` following the naming convention in `testing-patterns.md`:
   - Class: `{HandlerName}Tests` (sealed)
   - Methods: `Handle_{Condition}_{ExpectedOutcome}`
2. Cover at minimum: happy path, not-found path (if handler can fail), and any
   `DomainException` paths.
3. If the feature has new domain entities, add faker factory methods to
   `Application.Tests/Fakers/` following the `StravaDomainFakers` pattern.
4. If the feature introduces value objects with validation, add tests to
   `MarathonTraining.Domain.Tests/`.

---

## Step 7 — Integration tests

In `api/tests/MarathonTraining.Api.IntegrationTests/`:

1. Create `Features/{FeatureName}.feature` — at least a happy-path scenario and one
   error scenario. Follow the Gherkin structure in `testing-patterns.md`.
2. Create `Support/{FeatureName}Context.cs` — POCO holding Factory, HttpClient, and
   any scenario-specific state.
3. Create `Hooks/{FeatureName}Hooks.cs` — `[BeforeScenario]` creates factory + client,
   wipes relevant DB tables; `[AfterScenario]` disposes all.
4. Create `Steps/{FeatureName}Steps.cs` — Given/When/Then bindings.
5. Follow the `ApiWebApplicationFactory` usage pattern from `testing-patterns.md`.
6. If the feature calls an external HTTP service, set up WireMock stubs in the `[When]` step.

---

## Step 8 — Build and test

```bash
dotnet build api/
dotnet test api/ --logger "console;verbosity=normal"
```

Fix any compiler errors or test failures before proceeding. Do not commit a red build.

---

## Step 9 — Commit

Stage only the files created or modified for this feature (do not include unrelated
changes). Commit with a conventional commit message:

```
feat: add {feature name in kebab-case}
```

Example: `feat: add training-week-summary endpoint with TSS aggregation`
