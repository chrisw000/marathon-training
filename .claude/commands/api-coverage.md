# /api-coverage — Report API endpoint integration test coverage gaps

**Usage:** `/api-coverage`

---

## Step 1 — Inventory all registered endpoints

Read `api/src/MarathonTraining.Api/Program.cs` and extract every `app.Map*` call.
For each endpoint record:

- HTTP method (`GET`, `POST`, `DELETE`, etc.)
- Route path
- `.WithName(...)` value (or MISSING if absent)
- `.WithTags(...)` value (or MISSING if absent)
- Whether `.AllowAnonymous()` is present
- The MediatR command or query dispatched via `sender.Send(...)`

---

## Step 2 — Inventory all Reqnroll feature files

List all `.feature` files under `api/tests/MarathonTraining.Api.IntegrationTests/Features/`:

```bash
ls api/tests/MarathonTraining.Api.IntegrationTests/Features/
```

For each feature file, read it and extract:
- The feature name
- Each scenario title
- The HTTP method + route used in `[When]` steps (look for `HttpClient` calls or route strings)

---

## Step 3 — Inventory step definition files

List all `*Steps.cs` files and check they have bindings for every scenario in the corresponding feature file:

```bash
ls api/tests/MarathonTraining.Api.IntegrationTests/Steps/
```

---

## Step 4 — Cross-reference endpoints against feature files

For each endpoint found in step 1:

1. **Integration test:** Does a `.feature` file exist that exercises this route with this HTTP method?
2. **Happy-path scenario:** Does at least one scenario test the 200/201/204 response path?
3. **Error scenario:** Does at least one scenario test an error path (400, 401, 404, etc.)?
4. **Auth scenario:** If the endpoint is authenticated, does any scenario verify that a missing/invalid token returns 401?

---

## Step 5 — Check OpenAPI metadata completeness

For each endpoint:

| Check | PASS condition |
|---|---|
| Has `.WithName()` | Named endpoint for NSwag schema generation |
| Has `.WithTags()` | Grouped under the correct feature tag |
| Uses `ISender` (not `IMediator`) | `ISender sender` parameter only |
| Auth endpoints use `GetObjectId()` | Not `FindFirst("sub")` or similar |
| Anonymous endpoints have `.AllowAnonymous()` | All unauthenticated routes opt out explicitly |

---

## Step 6 — Output the coverage report

Print a table:

```
## API Endpoint Coverage

| Method | Route | Auth | WithName | WithTags | Happy Path | Error Path | Auth Check | Status |
|---|---|---|---|---|---|---|---|---|
| POST | /api/profile | Yes | YES | YES | YES | NO | NO | PARTIAL |
| GET  | /api/strava/status | Yes | YES | YES | YES | NO | NO | PARTIAL |
| POST | /api/strava/authorise | Yes | YES | YES | YES | NO | NO | PARTIAL |
| POST | /api/strava/callback | No | YES | YES | YES | YES | — | COVERED |
| DELETE | /api/strava/disconnect | Yes | YES | YES | NO | NO | NO | MISSING |

## Feature File Inventory

| Feature File | Scenarios | Routes Exercised |
|---|---|---|
| AthleteProfile.feature | 2 | POST /api/profile |
| StravaAuth.feature | 3 | POST /api/strava/callback, GET /api/strava/status |

## OpenAPI Metadata Gaps

List any endpoint missing .WithName() or .WithTags() here.

## Coverage Gaps — Prioritised

{List gaps with priority:}

HIGH   — Authenticated write-path endpoints with no error or auth scenario
MEDIUM — Read-path endpoints missing error scenarios
LOW    — Anonymous endpoints or endpoints already covered indirectly
```

**Priority guidance:**
- `HIGH` — authenticated POST/DELETE endpoints with no 401 or 404 scenario (auth or data-loss risk)
- `MEDIUM` — GET endpoints with no error scenario (query path, lower risk)
- `LOW` — already-covered paths, anonymous endpoints, or health checks

End with a concrete recommendation: the single most important gap to close and a suggested scenario title.
