# /review — Review staged or recently changed files against all skill documents

**Usage:** `/review`

---

## Step 1 — Identify changed files

Run:
```bash
git diff --name-only HEAD        # unstaged changes
git diff --name-only --cached    # staged changes
```

If nothing is staged, review all files modified since the last commit.
Group the files by project:

- `api/src/MarathonTraining.Domain/**` → apply Domain checks
- `api/src/MarathonTraining.Application/**` → apply Application checks
- `api/src/MarathonTraining.Infrastructure/**` → apply Infrastructure checks
- `api/src/MarathonTraining.Api/**` → apply API checks
- `api/tests/**` → apply Testing checks
- `ui/src/**` → apply UI checks

---

## Step 2 — Read the relevant skill documents

Before evaluating any file, read every applicable skill document:
- `.claude/skills/ddd-conventions.md` — for Domain and Application files
- `.claude/skills/testing-patterns.md` — for any test project
- `.claude/skills/api-endpoints.md` — for Api project files
- `.claude/skills/strava-integration.md` — for any Strava-related changes
- `.claude/skills/ef-migrations.md` — for Infrastructure/Persistence changes
- `.claude/skills/ui-patterns.md` — for UI files

---

## Step 3 — Evaluate each file

For each changed file, run through the relevant checks below and record a result.

### Domain / Application checks (`ddd-conventions.md`)

| Check | PASS condition |
|---|---|
| Aggregate has private setters | All properties `{ get; private set; }` |
| Aggregate has protected EF constructor | `protected {ClassName}() { }` present |
| Value object uses static Create factory | `private` constructor + `static Create(...)` |
| Value object validates in Create, throws DomainException | Invalid input → `throw new DomainException(...)` |
| No EF attributes on domain classes | No `[Key]`, `[Required]`, `[Column]`, `[Table]` |
| No MediatR in Domain project | No `using MediatR;` in Domain files |
| Repository interface in Domain.Interfaces | `I{Name}Repository` lives in `Domain/Interfaces/` |
| Handler is sealed class with primary-constructor DI | `public sealed class ... (IDep dep) : IRequestHandler<>` |
| Command/query is a record | `public sealed record {Name}Command(...)` |

### Testing checks (`testing-patterns.md`)

| Check | PASS condition |
|---|---|
| Test class naming | `{Subject}Tests`, sealed |
| Method naming | `{Method}_{Condition}_{ExpectedOutcome}` |
| NSubstitute used (not Moq) | `Substitute.For<T>()` only |
| Mocks created as fields | Not inside `[Fact]` methods |
| SUT constructed in constructor | Not in `[Fact]` |
| AwesomeAssertions used | `.Should()` chained, no `Assert.` calls |
| Bogus faker used for test data | `StravaDomainFakers` or inline `new Faker()` |
| Fresh scope for DB assertions | `context.Factory!.Services.CreateAsyncScope()` |
| WireMock stub set up in [When] step | Not in [Given] or [BeforeScenario] |

### API checks (`api-endpoints.md`)

| Check | PASS condition |
|---|---|
| Endpoint uses ISender, not IMediator | `ISender sender` parameter |
| User ID extracted via GetObjectId() | `user.GetObjectId() ?? throw ...` |
| Anonymous endpoints have .AllowAnonymous() | Present on `/health`, `/api/strava/callback` etc. |
| All endpoints have .WithName() and .WithTags() | Both present |
| Endpoint reference table updated | New endpoint appears in `api-endpoints.md` |
| No business logic in endpoint lambda | Logic delegated to MediatR handler |

### Infrastructure / EF checks (`ef-migrations.md`)

| Check | PASS condition |
|---|---|
| EF configuration in OnModelCreating | No data annotations on domain classes |
| Indexes configured for unique constraints | `entity.HasIndex(...).IsUnique()` |
| New DbSet added to AppDbContext | If new aggregate added to schema |
| Repository implements domain interface | `public sealed class XRepository : IXRepository` |

### UI checks (`ui-patterns.md`)

| Check | PASS condition |
|---|---|
| pnpm used (not npm) | No `package-lock.json` present |
| API calls go through marathonApi.ts | No raw `fetch` in components |
| Bearer token via getAccessToken() | `apiRequest` helper used, not manual header |
| useAuth() used (not useMsal() directly) | No `import { useMsal }` in pages/ |
| No secrets in VITE_ variables | Only non-secret config values |

### Security checks (all files)

| Check | PASS condition |
|---|---|
| No secrets committed | No API keys, passwords, tokens in any file |
| No .env.local or user-secrets file staged | Not in `git diff --cached` output |
| SQL queries parameterised | No string interpolation in EF raw SQL |

---

## Step 4 — Pending commit message check

If there is a pending commit message (check with `git log --oneline -1` or the staged state),
verify it follows conventional commits:

| Check | PASS condition |
|---|---|
| Conventional commit prefix | Starts with `feat:`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:` |
| Imperative mood | "add", "fix", "remove" — not "added", "fixing" |
| Under 72 characters | First line ≤ 72 chars |

---

## Step 5 — Output

Print a checklist. Use exactly these markers:

- `PASS` — check satisfied
- `WARN` — minor issue, can proceed but should fix soon
- `FAIL` — must fix before committing

Format:
```
## Review: {filename}

- [PASS] Aggregate has private setters
- [WARN] Missing .WithTags() on new endpoint — add before merging
- [FAIL] No unit test found for GetTrainingWeeksQueryHandler
```

Print a summary at the end:
```
## Summary
Total: X PASS, Y WARN, Z FAIL
{Action}: Ready to commit / Fix FAIL items before committing
```
