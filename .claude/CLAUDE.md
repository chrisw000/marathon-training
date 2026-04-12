# Marathon Trainer — Claude Code Agent File

## Project overview

**What it does:** A single-user marathon training tracker that pulls activities from Strava,
models training load using TSS (Training Stress Score), and presents a unified view of runs,
rides and strength sessions. Goal: help one athlete (aged 52, targeting sub-3:30 marathon) plan
and track their training load intelligently.

**Target user:** A single authenticated athlete. There is no multi-tenancy, no public
registration, no admin panel. Every data-access decision is made in that context.

**Current state:** Authentication (Microsoft Entra External ID), athlete profile creation, and
Strava OAuth connect/disconnect flow are complete. Activity sync and TSS modelling are not yet
implemented.

### Tech stack

| Layer | Technology | Version |
|---|---|---|
| API runtime | .NET / ASP.NET Core | net10.0 |
| Auth | Microsoft.Identity.Web | 4.7.0 |
| JWT validation | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.5 |
| CQRS | MediatR | 14.1.0 |
| Validation | FluentValidation | 12.1.1 |
| ORM | EF Core (SQL Server provider) | 10.0.5 |
| OpenAPI | Microsoft.AspNetCore.OpenApi + NSwag | 10.0.5 / 14.7.0 |
| UI runtime | React | 19.2.4 |
| UI language | TypeScript | ~6.0.2 |
| UI build | Vite + @vitejs/plugin-react-swc | 8.0.8 |
| Auth (UI) | @azure/msal-browser + @azure/msal-react | 5.6.3 / 5.2.1 |
| Data fetching | @tanstack/react-query | 5.97.0 |
| Routing | react-router-dom | 7.14.0 |
| Package manager | pnpm | 10.33.0 |
| Unit tests | xUnit + NSubstitute + AwesomeAssertions + Bogus | 2.9.3 / 5.3.0 / 9.4.0 / 35.6.5 |
| Integration tests | Reqnroll + WireMock.Net + Testcontainers.MsSql | 3.3.4 / 2.2.0 / 4.4.0 |
| UI tests | Vitest + @testing-library/react | 4.1.4 / 16.3.2 |

---

## Repository structure

```
marathon.trainer/
├── api/
│   ├── src/
│   │   ├── MarathonTraining.Api/           # Entry point — Program.cs, DI wiring, endpoint registration
│   │   ├── MarathonTraining.Application/   # MediatR commands, queries, handlers, DTOs
│   │   ├── MarathonTraining.Domain/        # Aggregates, value objects, interfaces, exceptions
│   │   └── MarathonTraining.Infrastructure/# EF Core DbContext, repositories, Strava HTTP client
│   └── tests/
│       ├── MarathonTraining.Api.IntegrationTests/  # Reqnroll BDD + WireMock + Testcontainers
│       ├── MarathonTraining.Application.Tests/     # xUnit handler unit tests
│       ├── MarathonTraining.Domain.Tests/          # (placeholder — domain logic tests go here)
│       └── MarathonTraining.Infrastructure.Tests/  # (placeholder)
├── ui/
│   └── src/
│       ├── auth/       # msalConfig.ts, AuthProvider.tsx, useAuth.ts
│       ├── api/        # marathonApi.ts — React Query hooks over the REST API
│       ├── pages/      # Route-level components (LoginPage, HomePage, StravaConnectedPage)
│       ├── App.tsx     # Router and ProtectedRoute wrapper
│       └── main.tsx    # Entry point — wraps app in AuthProvider + QueryClientProvider
└── .claude/
    ├── CLAUDE.md          # This file
    ├── skills/            # Reference docs for specific concerns
    └── commands/          # Reusable slash command prompts
```

**Two-repo split:** `api/` and `ui/` are entirely separate build systems. Run API with
`dotnet run`, run UI with `pnpm dev`. They communicate only over HTTP.

---

## Standing conventions

### Package manager
**Always use pnpm** (never npm or yarn) for all UI operations.
`package.json` declares `"packageManager": "pnpm@10.33.0"`.

### Local development URLs
| Service | URL | How to start |
|---|---|---|
| API (http profile) | `http://localhost:5259` | `dotnet run --launch-profile http` from `api/src/MarathonTraining.Api/` |
| API (Docker) | `http://localhost:5000` | `docker compose up` |
| UI dev server | `http://localhost:5173` | `pnpm dev` from `ui/` |

**Never use HTTPS for local development.** The API's HTTPS redirect is active but the
`--launch-profile http` profile binds to HTTP only. The UI's `.env.local` points at
`http://localhost:5259`.

### Secrets management
- **API:** `dotnet user-secrets` (UserSecretsId: `e108429e-d058-4c3a-935b-ad29e468926c`)  
  Keys: `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:Audience`, `Strava:ClientId`,
  `Strava:ClientSecret`, `Strava:RedirectUri`, `ConnectionStrings:DefaultConnection`
- **UI:** `.env.local` (gitignored). See `ui/.env.example` for required keys.
- **Never commit secrets.** Neither file is tracked by git.

### Git commit format
Conventional commits: `feat:`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:`  
Example: `feat: add weekly TSS summary endpoint`

---

## Architecture rules

### Dependency direction
```
Domain ← Application ← Infrastructure ← Api
```
- **Domain** has zero external package dependencies. No EF attributes, no MediatR, no HTTP.
- **Application** depends only on Domain. Defines interfaces it needs (repositories,
  external services). Does not reference Infrastructure.
- **Infrastructure** implements Application interfaces. References EF Core, HTTP clients.
- **Api** wires everything together in `Program.cs`. Registers DI, maps endpoints.

### Use case rules
- Every use case is a MediatR `IRequest` / `IRequestHandler`. No business logic in endpoints.
- Commands are `record` types implementing `IRequest` or `IRequest<TResult>`.
- Handlers are `sealed class` with primary-constructor dependency injection.
- The `ISender` interface (not `IMediator`) is injected into endpoints.

### Auth rules
- The fallback authorization policy requires an authenticated user — all endpoints are
  protected by default.
- Anonymous endpoints must explicitly call `.AllowAnonymous()` (e.g. `/health`, `/api/strava/callback`).
- User identity is extracted via `user.GetObjectId()` (Microsoft.Identity.Web extension),
  which reads the `oid` claim from the Entra token.

### Strava identity rule
Users authenticate with **Microsoft Entra External ID**, not Strava. Strava is a data source
accessed via OAuth. An `AthleteProfile` (keyed on the Entra `oid`) must exist before any
Strava operation can proceed.

---

## Key decisions log

### Why Microsoft Entra External ID (not Auth0)
Native Azure integration, no per-MAU pricing at low volumes, CIAM authority endpoint
(`*.ciamlogin.com`) purpose-built for customer-facing apps. Microsoft.Identity.Web handles
JWT validation with minimal config.

### Why Minimal API (not controllers)
Small surface area — a single-user app with <10 endpoints doesn't justify controller ceremony.
Endpoints live in `Program.cs` grouped by feature tags. If the file grows unwieldy, extract to
extension methods on `WebApplication`.

### Why MediatR for CQRS
Keeps handlers thin, testable in isolation, and decoupled from the HTTP layer. Each use case is
a self-contained class. Enables adding pipeline behaviours (logging, validation) without
touching handler code.

### Why SQL Server / Azure SQL Edge (not Postgres)
EF Core's SQL Server provider is the natural fit alongside the Microsoft stack. Azure SQL Edge
ships a multi-arch Docker image (`linux/arm64` + `amd64`) — unlike the standard SQL Server 2022
image which is `amd64`-only and fails under QEMU on ARM64 hosts. Wire-protocol-compatible with
full SQL Server, so EF Core's `UseSqlServer` works unchanged.

### Why pnpm (not npm)
Faster installs, strict dependency isolation, disk-efficient content-addressable store.
Declared explicitly as the `packageManager` in `package.json`.

### Why EnsureCreatedAsync (not migrations) in Development
Simple schema management during early development. `EnsureCreatedAsync` is a no-op if the
schema already exists. Will be replaced with migrations before any multi-environment deployment.

### Why /api/strava/authorise returns JSON (not a redirect)
A `fetch()` call carrying an `Authorization: Bearer` header cannot follow a cross-origin
redirect — the browser drops the header and the CORS preflight fails. The endpoint returns
`{ url }` and the SPA navigates with `window.location.href = url`.

---

## Coverage and health check commands

All commands live in `.claude/commands/`. Run them by typing the slash command in this session.

| Command | When to run |
|---|---|
| `/test-coverage` | After adding or changing application/domain handlers or value objects |
| `/api-coverage` | After adding or changing API endpoints or integration test feature files |
| `/ui-coverage` | After adding or changing React components, hooks, or API client functions |
| `/health-check` | Before every PR and at the end of every coding session |
| `/review` | Before every commit |

**Output location:** `docs/coverage/` — generated reports are gitignored except
`health-check-latest.txt`, which is committed to provide a quick health history via `git log`.
