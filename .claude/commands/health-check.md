# /health-check — Master project health check: builds, tests, coverage, conventions

**Usage:** `/health-check`

Run before every PR and at the end of every coding session.
Produces a full report in `docs/coverage/health-check.md` and a one-line status
in `docs/coverage/health-check-latest.txt`.

---

## Step 1 — Build verification

Run each build target and capture pass/fail plus any error output:

```bash
dotnet build api/MarathonTraining.slnx --no-incremental 2>&1
```

```bash
cd ui && pnpm run build 2>&1
```

```bash
cd ui && pnpm run type-check 2>&1
```

Record for each: PASS (exit 0, zero errors) or FAIL (non-zero exit or error lines present).
Capture the first 10 error lines if a build fails — do not print the full output.

---

## Step 2 — Test suites

Run each test suite and capture pass count, fail count, skip count, and duration:

```bash
dotnet test api/MarathonTraining.slnx --logger "console;verbosity=normal" 2>&1
```

```bash
cd ui && pnpm run test -- --run 2>&1
```

Parse the output to extract per-suite results. For .NET, look for lines like
`Passed: N, Failed: N, Skipped: N` per project. For Vitest, look for the summary
line `Tests N passed (N)`.

If any test suite fails to run (e.g. missing Docker for Testcontainers), record
the suite as SKIP with the reason noted.

---

## Step 3 — Coverage analysis

Run each coverage command internally (do not re-run build or tests — read the
source directly as those commands do) and extract only the **Summary** section
from each:

- **Unit test + handler coverage** — apply the steps from `.claude/commands/test-coverage.md`
  and collect the handler coverage table and coverage gap count.
- **API endpoint coverage** — apply the steps from `.claude/commands/api-coverage.md`
  and collect the endpoint coverage table and gap count.
- **UI coverage** — apply the steps from `.claude/commands/ui-coverage.md`
  and collect the component, hook, and API function counts.

Derive percentage values:

- `unit %` = (handlers with unit test / total handlers) × 100
- `api %` = (endpoints with at least happy-path integration test / total endpoints) × 100
- `ui-vitest %` = (components with Vitest test / total components) × 100
- `ui-storybook %` = (components with stories / total components) × 100
- `hooks %` = (hooks with test / total hooks) × 100

---

## Step 4 — Convention audit

Perform static analysis only — no test execution. Read source files directly.

### 4a — TODO / FIXME comments

```bash
grep -rn "TODO\|FIXME" api/src/ ui/src/ --include="*.cs" --include="*.ts" --include="*.tsx"
```

Collect each match as `{file}:{line} — {comment text}`.

### 4b — Hardcoded localhost URLs

```bash
grep -rn "localhost" api/src/ ui/src/ --include="*.cs" --include="*.ts" --include="*.tsx"
```

Flag any match that is NOT in:
- `appsettings.Development.json`
- `ui/.env.local`
- `ui/.env.example`
- Test infrastructure files (e.g. `ApiWebApplicationFactory.cs`)

### 4c — Secrets patterns

```bash
grep -rni "password=\|secret=\|apikey=\|api_key=\|client_secret=" api/ ui/ \
  --include="*.cs" --include="*.ts" --include="*.tsx" --include="*.json" \
  --exclude="*.example*"
```

Exclude `*.example` files. Any hit outside of comments is a FAIL.

### 4d — Dependency direction violations

Check that no file in `MarathonTraining.Domain` or `MarathonTraining.Application`
references `MarathonTraining.Infrastructure`:

```bash
grep -rn "MarathonTraining.Infrastructure" \
  api/src/MarathonTraining.Domain/ \
  api/src/MarathonTraining.Application/ \
  --include="*.cs"
```

Any match is a FAIL.

### 4e — pnpm / npm mixing

```bash
ls ui/package-lock.json 2>/dev/null && echo FOUND || echo CLEAN
```

`package-lock.json` present alongside `pnpm-lock.yaml` → FAIL.

### 4f — Conventional commits (last 5)

```bash
git log --oneline -5
```

For each message check: starts with `feat:`, `fix:`, `chore:`, `test:`, `docs:`,
or `refactor:`. Any message that does not → WARN.

---

## Step 5 — Compile and save the full report

Create `docs/coverage/` if it does not exist.
Save the report to `docs/coverage/health-check.md` (overwrite if present):

```markdown
# Project health check
Generated: {date}
Commit: {git log --oneline -1 output}

## Build status

| Target | Status | Details |
|---|---|---|
| .NET solution | PASS/FAIL | "clean" or first N error lines |
| UI build | PASS/FAIL | "clean" or first N error lines |
| TypeScript | PASS/FAIL | "clean" or error count |

## Test results

| Suite | Passed | Failed | Skipped | Duration |
|---|---|---|---|---|
| Domain.Tests | | | | |
| Application.Tests | | | | |
| Infrastructure.Tests | | | | |
| Api.IntegrationTests | | | | |
| UI (Vitest) | | | | |

## Coverage summary

| Area | Covered | Total | % | Status |
|---|---|---|---|---|
| Unit tests (handlers) | | | | GOOD / NEEDS WORK / AT RISK |
| API endpoints (integration) | | | | |
| UI components (Vitest) | | | | |
| UI components (Storybook) | | | | |
| Custom hooks | | | | |

Coverage thresholds: ≥ 80% = GOOD, 60–79% = NEEDS WORK, < 60% = AT RISK

## Convention audit

| Check | Status | Details |
|---|---|---|
| No TODO/FIXME in src | PASS/WARN | N found: {list} |
| No hardcoded localhost URLs | PASS/WARN | N found: {list} |
| No committed secrets | PASS/FAIL | "clean" or {file:line} |
| Dependency directions correct | PASS/FAIL | "clean" or {file:line} |
| pnpm only (no npm mix) | PASS/FAIL | "clean" or "package-lock.json found" |
| Conventional commits (last 5) | PASS/WARN | N non-conforming: {list} |

## Action items

List every FAIL and WARN item:

| Severity | Issue | File(s) | Suggested fix |
|---|---|---|---|
| FAIL | ... | ... | ... |
| WARN | ... | ... | ... |

Sorted: FAIL first, then WARN.

## Overall health

HEALTHY — all checks PASS and all coverage areas ≥ 80%
NEEDS ATTENTION — any WARN present, or any coverage area 60–79%
AT RISK — any FAIL present, or any coverage area < 60%
```

---

## Step 6 — Save the one-line status

Append (do not overwrite) to `docs/coverage/health-check-latest.txt`:

```
{date} | {overall status} | Build: {PASS/FAIL} | Tests: {N passed, N failed} | Coverage: unit {N}% / api {N}% / ui {N}%
```

This file is committed to git — it gives a quick health history via `git log`.

---

## Step 7 — Print summary to console

Print the **Build status**, **Test results**, **Coverage summary**, **Convention audit**,
and **Overall health** sections to the console. Do not print the full action items list —
just the count (`N FAIL, N WARN — see docs/coverage/health-check.md`).
