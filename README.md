# Marathon Trainer

A full-stack application to help runners plan, track, and optimize their marathon training.

## Structure

- `api/` — Backend API (.NET 10, clean architecture)
- `ui/` — Frontend application (React + TypeScript + Vite)

## Getting Started

See the README in each subdirectory for setup instructions.

---

## Prerequisites

| Tool | Required for |
|---|---|
| .NET 10 SDK | Building and running the API |
| Node.js + pnpm | Building and running the UI |
| Docker Desktop | Running integration tests (Testcontainers pulls `mcr.microsoft.com/azure-sql-edge:latest` automatically) |

Docker Desktop must be **running** when you execute `dotnet test`. The integration test project (`MarathonTraining.Api.IntegrationTests`) starts a SQL Server container via Testcontainers — no manual container setup is needed, but the Docker daemon must be available.

---

## Verifying the solution

### Local dev ports

| Service | Local dev URL | Notes |
|---|---|---|
| .NET API | `http://localhost:5259` | Selected via `--launch-profile http` in `launchSettings.json` |
| Vite UI | `http://localhost:5173` | Default Vite port |
| SQL Server (Docker) | `localhost:1433` | |

> **Port 5000 is Docker-only.** It is the port the API listens on inside the container (`ASPNETCORE_URLS=http://+:5000`), exposed by `docker-compose.yml`. It is not used when running the API locally with `dotnet run`.

To run the API locally:

```bash
dotnet run --project api/src/MarathonTraining.Api --launch-profile http
```

### Build and test verification prompt

Use the following prompt to ask Claude to verify the full solution builds and all tests pass:

```
Verify the entire solution builds and all tests pass with zero failures:

1. Run `dotnet build api/MarathonTraining.slnx` and confirm no errors
2. Run `dotnet test api/MarathonTraining.slnx` and confirm all test projects are discovered and pass
   (Docker Desktop must be running — the integration tests pull mcr.microsoft.com/azure-sql-edge:latest
   via Testcontainers; if any project has 0 tests, add a placeholder test)
3. Run `cd ui && pnpm install && pnpm run build` and confirm no TypeScript errors
4. Run `pnpm test:run` and confirm all tests are discovered and pass

If anything fails, fix it before moving on. Report the final status of each step.
```

### Test architecture

| Project | Type | What it covers |
|---|---|---|
| `MarathonTraining.Domain.Tests` | Unit | Domain model invariants |
| `MarathonTraining.Infrastructure.Tests` | Unit | Infrastructure utilities |
| `MarathonTraining.Application.Tests` | Unit | MediatR command/query handlers (NSubstitute mocks, no DB) |
| `MarathonTraining.Api.IntegrationTests` | Integration (Reqnroll BDD) | Full Strava OAuth callback flow end-to-end |

The integration test project uses:
- **Testcontainers** — starts `mcr.microsoft.com/azure-sql-edge:latest` once per test run (multi-arch image, works on both `linux/amd64` and `linux/arm64`). The standard `mcr.microsoft.com/mssql/server` image is `amd64`-only and fails under QEMU on ARM64 hosts.
- **WireMock.Net** — intercepts `POST /oauth/token` calls to Strava, one server per scenario.
- **WebApplicationFactory** — hosts the API in-process with the Testcontainer DB and WireMock base URL injected via `ConfigureTestServices`.

### Auth flow verification prompt

Use the following prompt to ask Claude to verify the auth endpoints behave correctly:

```
Verify the auth flow end to end:

1. Start the .NET API with `dotnet run --project api/src/MarathonTraining.Api --launch-profile http` and confirm it starts on http://localhost:5259
2. Start the Vite dev server with `cd ui && pnpm run dev` and confirm it starts on http://localhost:5173
3. Check that GET http://localhost:5259/health returns 200 OK
4. Check that GET http://localhost:5259/me without a token returns 401

Stop both servers when done. Report the result of each check.
```

---

## Dependency policy

These rules apply to both `api/` and `ui/` and are enforced at the tooling level where possible.

### No prerelease packages

All dependencies — direct and transitive — must be fully released versions (no `alpha`, `beta`, `rc`, `preview`, `canary`, or `next` suffixes). This applies to NuGet packages and npm packages alike.

**Why:** Prerelease packages can change their API or behaviour without notice. More importantly, they represent a wider supply-chain attack surface — a compromised prerelease publish is less likely to be caught quickly by the community.

**What to do when a desired package is only available as a prerelease:**
- Pin to the latest fully released version of that package.
- If the package has never had a stable release, find an alternative.
- Do not add an exclusion to work around this policy without explicit sign-off.

### npm — 7-day release cooldown (ui/)

pnpm is configured with `minimum-release-age=10080` (7 days × 1440 min/day) in `ui/.npmrc`. pnpm will refuse to resolve any package version published within the last 7 days, even if it satisfies the `package.json` semver range.

**Why:** Many supply-chain attacks (account takeover, typosquatting, malicious patch bumps) are caught within the first week of a malicious publish. A 7-day window gives the community time to identify and report problems before they reach this codebase.

**The cooldown applies to all packages — stable and prerelease alike.** Do not add `minimum-release-age-exclude` entries without explicit sign-off. If a package is blocked by the cooldown, wait.

### Approved prerelease exceptions (ui/)

Certain prerelease packages are explicitly approved for use. They remain subject to the 7-day cooldown — only the prerelease restriction is waived, not the time rule.

| Package | Approved version range | Reason |
|---|---|---|
| `rolldown` + `@rolldown/*` | Any RC | vite 8 uses rolldown as its bundler; rolldown ships RC versions while its API stabilises. Approved for use. Revisit once rolldown reaches 1.0.0 stable. |

### Keeping packages up to date

When upgrading, prefer the latest stable release of each package. If upgrading a package would introduce an unapproved prerelease transitive dependency, pin to the last clean version and note why in the PR description.

#### Current pinned packages (ui/)

| Package | Pinned at | Latest | Reason |
|---|---|---|---|
| `eslint-plugin-react-hooks` | `4.6.2` | `7.x` | `7.0.0` introduced `@babel/core` as a direct dependency, pulling in `gensync@1.0.0-beta.2`. |
