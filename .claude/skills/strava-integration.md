# Strava Integration

---

## OAuth flow summary

1. **SPA calls `GET /api/strava/authorise`** (Bearer token required)
   - API generates a random state token via `IStravaOAuthStateService.GenerateState(userId)`
   - Returns `{ url }` — the full Strava OAuth consent URL

2. **SPA navigates with `window.location.href = url`**
   - Strava shows the consent page
   - On approval, Strava redirects to the registered `redirect_uri`

3. **Browser follows redirect to `GET /api/strava/callback?code=...&state=...`** (Anonymous)
   - API validates and consumes the state token → recovers `userId`
   - Dispatches `ConnectStravaCommand(userId, code)` via MediatR
   - Handler exchanges code for tokens via `IStravaTokenService.ExchangeCodeAsync`
   - Upserts `StravaConnection` for the athlete's profile
   - Redirects browser to `http://localhost:5173/strava-connected` (configurable via `Strava:UiSuccessRedirectUri`)

**Scopes requested:** `activity:read_all`  
**Redirect URI (local dev):** `http://localhost:5259/api/strava/callback`  
**Strava OAuth consent URL base:** `https://www.strava.com/oauth/authorize`  
**Token endpoint:** `POST https://www.strava.com/oauth/token`

---

## Token storage

Tokens are stored in the `StravaConnections` table as a `StravaConnection` aggregate:
- `AthleteProfileId` — FK to `AthleteProfiles` (also the PK — one connection per athlete)
- `StravaAthleteId` — Strava's numeric athlete ID
- `AccessToken` — current bearer token for Strava API calls
- `RefreshToken` — used to obtain a new access token after expiry
- `ExpiresAt` — UTC timestamp; access token is considered stale when `ExpiresAt <= UtcNow`

**Upsert semantics:** `StravaTokenRepository.UpsertAsync` inserts on first connect and calls
`StravaConnection.Update(...)` on reconnect (e.g. re-authorisation after revocation).

**Refresh trigger:** Not yet implemented. `GetStravaConnectionStatusQuery` reports
`IsConnected = false` when the token is expired; the UI must prompt reconnect.
`IStravaTokenService.RefreshTokenAsync` exists but is not yet wired to any handler.

---

## IStravaTokenService contract

```csharp
public interface IStravaTokenService
{
    // Exchange an OAuth authorisation code for access/refresh tokens.
    Task<StravaTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);

    // Refresh an expired access token using the stored refresh token.
    // Strava does not return athlete data on refresh — existingStravaAthleteId is carried through.
    Task<StravaTokenResponse> RefreshTokenAsync(string refreshToken, long existingStravaAthleteId, CancellationToken cancellationToken = default);
}

public record StravaTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    long StravaAthleteId);
```

The concrete implementation (`StravaTokenService`) is registered as an HTTP-client-backed
service with base address `https://www.strava.com/`. In tests the base address is replaced
with the WireMock server URL.

---

## IStravaOAuthStateService contract

```csharp
public interface IStravaOAuthStateService
{
    string GenerateState(string userId);           // Creates and stores a GUID state token → userId mapping
    string? ValidateAndConsumeState(string state); // Validates + removes the token; returns userId or null
}
```

Current implementation: `InMemoryStravaOAuthStateService` — a `ConcurrentDictionary` with a
10-minute TTL. Registered as a **singleton** so state survives across requests.

**Limitation:** Does not survive API restarts or horizontal scale-out. Acceptable for single-
instance development; replace with a distributed cache (Redis) before multi-instance deployment.

---

## WireMock stubs defined in tests

### Successful token exchange (200)
```csharp
var tokenResponseBody = JsonSerializer.Serialize(new
{
    access_token = Fake.Random.AlphaNumeric(40),
    refresh_token = Fake.Random.AlphaNumeric(40),
    expires_at = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
    athlete = new { id = Fake.Random.Long(1_000_000, 9_999_999) },
});

context.WireMockServer!
    .Given(Request.Create().WithPath("/oauth/token").UsingPost())
    .RespondWith(Response.Create()
        .WithStatusCode(200)
        .WithHeader("Content-Type", "application/json")
        .WithBody(tokenResponseBody));
```

### Failed token exchange (400)
```csharp
context.WireMockServer!
    .Given(Request.Create().WithPath("/oauth/token").UsingPost())
    .RespondWith(Response.Create()
        .WithStatusCode(400)
        .WithHeader("Content-Type", "application/json")
        .WithBody(errorResponseBody));
```

---

## Activity sync — current state

**Implemented.** The full sync pipeline is live:

- `POST /api/activities/sync` — triggers Strava sync for the authenticated athlete
  - Handler: `SyncStravaActivitiesCommandHandler`
  - Paginates through `GET /api/v3/athlete/activities` (100 per page)
  - Incremental after first sync (uses `profile.LastSyncedAt` as `after` epoch)
  - Auto-refreshes token if expiring within 5 minutes
  - Returns `{ activitiesSynced, activitiesSkipped, syncedAt }`
  - 422 if no Strava connection; 429 propagated from Strava rate limit

- `POST /api/activities/manual` — logs a non-Strava activity (strength sessions)
  - Handler: `LogManualActivityCommandHandler`
  - Validates: ActivityType must be Strength, duration 1–479 min, RPE 1–10
  - Returns 201 with `{ activityId, tssScore }`

- `GET /api/activities` — paginated activity list with optional filters
  - Query params: `type` (Run/Ride/Strength), `from`, `to`, `page`, `pageSize`
  - Returns `{ items, totalCount, page, pageSize, totalPages, hasNextPage, hasPreviousPage }`

**Domain mapping:**  
Strava `sport_type` → `ActivityType` enum: `Run` | `Ride` | `Strength` (via `ActivityTypeMapper`)  
Strava `moving_time` → `Activity.DurationSeconds`  
Strava `distance` (metres) → `Activity.DistanceMetres`  
TSS is calculated immediately after sync using `ITssCalculationService`.

**UI hooks (marathonApi.ts):**  
- `useSyncActivities()` — mutation for `POST /api/activities/sync`
- `useActivities(params?)` — query for `GET /api/activities` with optional type/page filters

**EF Core note:** `TrainingWeekRepository.UpdateAsync` explicitly sets new `Activity` entities
to `EntityState.Added` before `SaveChangesAsync`. EF Core's `DetectChanges` marks navigation-
discovered entities with non-default Guid keys as `Modified` instead of `Added`, which causes
`DbUpdateConcurrencyException`. Always use this pattern when adding child entities to a
tracked aggregate.

---

## Configuration keys

Set via `dotnet user-secrets` for local development:

| Key | Description |
|---|---|
| `Strava:ClientId` | App's numeric client ID from Strava API settings |
| `Strava:ClientSecret` | App's client secret |
| `Strava:RedirectUri` | Must match Strava API settings exactly (e.g. `http://localhost:5259/api/strava/callback`) |
| `Strava:UiSuccessRedirectUri` | Where to redirect the browser after successful connect (default: `http://localhost:5173/strava-connected`) |
