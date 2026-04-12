# /test-coverage — Report handler and value-object test coverage gaps

**Usage:** `/test-coverage`

---

## Step 1 — Find all MediatR handlers

Search for all classes implementing `IRequestHandler` in the Application layer:

```bash
grep -r "IRequestHandler" api/src/MarathonTraining.Application/ --include="*.cs" -l
```

Read each file to identify the handler class name and what command/query it handles.

---

## Step 2 — Find all unit test classes

Search for all test classes in `MarathonTraining.Application.Tests`:

```bash
grep -r "class.*Tests" api/tests/MarathonTraining.Application.Tests/ --include="*.cs" -l
```

---

## Step 3 — Find all domain value objects

Search for value objects (records with static Create factory) in the Domain layer:

```bash
grep -r "static.*Create(" api/src/MarathonTraining.Domain/ --include="*.cs" -l
```

---

## Step 4 — Find all integration test feature files

```bash
ls api/tests/MarathonTraining.Api.IntegrationTests/Features/
```

---

## Step 5 — Cross-reference

For each handler found in step 1, check:
1. **Unit test:** Does a class named `{HandlerName}Tests` exist in `Application.Tests/`?
2. **Integration test:** Does a `.feature` file exist that exercises the corresponding endpoint?

For each value object found in step 3, check:
1. Does a test in `MarathonTraining.Domain.Tests/` exercise the `Create()` validation path?

---

## Step 6 — Output the coverage gap report

Print a table:

```
## Handler Coverage

| Handler | Command/Query | Has Unit Test | Has Integration Test | Priority |
|---|---|---|---|---|
| EnsureAthleteProfileCommandHandler | EnsureAthleteProfileCommand | YES | YES (AthleteProfile.feature) | - |
| ConnectStravaCommandHandler | ConnectStravaCommand | YES | YES (StravaAuth.feature) | - |
| DisconnectStravaCommandHandler | DisconnectStravaCommand | YES | NO | Medium |
| GetStravaConnectionStatusQueryHandler | GetStravaConnectionStatusQuery | YES | NO | Low |

## Value Object Coverage

| Value Object | Has Validation Test |
|---|---|
| TssScore | NO |

## Coverage Gaps — Prioritised

{List gaps with suggested priority:}

HIGH   — Core user flows with no integration test (data loss risk)
MEDIUM — Secondary flows with no integration test  
LOW    — Query-only paths already covered indirectly by feature tests
```

**Priority guidance:**
- `HIGH` — write-path handlers (commands that mutate state) with no integration test
- `MEDIUM` — read-path handlers (queries) with no integration test
- `LOW` — handlers already exercised indirectly by integration tests that call the same endpoint
- `LOW` — value objects with only a single valid state (little to validate)

End with a concrete recommendation: which gap to close first and why.
