# API Endpoints

---

## Where endpoints are registered

All endpoints are registered directly in `api/src/MarathonTraining.Api/Program.cs`.
There are no controller classes.

If `Program.cs` grows unwieldy, extract feature groups to extension methods on `WebApplication`:
```csharp
// e.g. app.MapTrainingWeekEndpoints();
```
But do not do this prematurely — the file currently fits comfortably.

---

## Auth pattern

**Default policy:** The fallback authorization policy requires an authenticated user.
All endpoints require auth unless they call `.AllowAnonymous()`.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

**Extracting the user ID:**
```csharp
var userId = user.GetObjectId()
    ?? throw new InvalidOperationException("No object ID claim found on the authenticated user.");
```
`GetObjectId()` is a Microsoft.Identity.Web extension that reads the `oid` claim from the
Entra JWT. `ClaimsPrincipal user` is injected as a parameter by the minimal API framework.

**Marking an endpoint anonymous:**
```csharp
app.MapGet("/health", () => Results.Ok())
   .AllowAnonymous();
```

---

## ISender injection pattern

MediatR's `ISender` is injected as a parameter — not `IMediator`:
```csharp
app.MapPost("/api/profile", async (ClaimsPrincipal user, ISender sender) =>
{
    var userId = user.GetObjectId() ?? throw new InvalidOperationException(...);
    var result = await sender.Send(new EnsureAthleteProfileCommand(userId, displayName));
    return Results.Ok(new { created = result.WasCreated });
});
```

---

## Request/response DTO conventions

- **Commands and queries** are C# `record` types co-located with their handlers
  in `Application/{Feature}/{Name}Command.cs` or `{Name}Query.cs`
- **Result DTOs** are `record` types in the same file as the command/query
- **Response shapes** returned from endpoints are either the result DTO directly or an
  anonymous `new { ... }` object for simple cases
- **No separate "DTO folder"** — DTOs live next to the handler that produces or consumes them

---

## Error response shape

`DomainException` is caught at the endpoint level and translated to a `400 Bad Request`
with a JSON body:
```csharp
catch (DomainException)
{
    return Results.BadRequest(new { error = "invalid_code" });
}
```

For unhandled exceptions, ASP.NET Core's default problem details middleware is not yet
configured — this is a future improvement.

---

## Route naming convention

```
/api/{resource}         — collection or singleton resource
/api/{resource}/{action} — non-CRUD action on a resource
```

Examples:
- `POST /api/profile` — ensure athlete profile exists
- `GET /api/strava/status` — get Strava connection status
- `GET /api/strava/authorise` — get Strava OAuth URL
- `GET /api/strava/callback` — anonymous Strava OAuth redirect handler
- `DELETE /api/strava/disconnect` — remove Strava connection

Use lowercase kebab-case in route strings.

---

## Endpoint reference table

| Method | Route | Auth | Handler / Description |
|---|---|---|---|
| GET | `/health` | Anonymous | Returns 200 OK — liveness probe |
| GET | `/me` | Required | Returns all claims from the JWT |
| POST | `/api/profile` | Required | `EnsureAthleteProfileCommand` — idempotent profile creation |
| GET | `/api/strava/authorise` | Required | Returns `{ url }` for Strava OAuth consent page |
| GET | `/api/strava/callback` | Anonymous | `ConnectStravaCommand` — exchanges code, stores tokens, redirects UI |
| DELETE | `/api/strava/disconnect` | Required | `DisconnectStravaCommand` — removes stored tokens |
| GET | `/api/strava/status` | Required | `GetStravaConnectionStatusQuery` — is Strava connected? |
| GET | `/api/athlete/profile` | Required | `GetAthleteProfileQuery` — current athlete profile with HR zones, FTP, training phase |
| PATCH | `/api/athlete/physiology` | Required | `UpdateAthletePhysiologyCommand` — update HR zones and FTP watts |
| PATCH | `/api/athlete/phase` | Required | `UpdateTrainingPhaseCommand` — update current training phase |
| POST | `/api/activities/sync` | Required | `SyncStravaActivitiesCommand` — incremental or full Strava sync; 422 if not connected, 429 on rate limit |
| POST | `/api/activities/manual` | Required | `LogManualActivityCommand` — log a manual Strength activity with RPE; 201 with `{ activityId, tssScore }` |
| GET | `/api/activities` | Required | `GetActivitiesQuery` — paginated list; optional `?type=`, `?from=`, `?to=`, `?page=`, `?pageSize=` |

**Update this table when adding new endpoints.**

---

## WithName and WithTags

Every endpoint should have `.WithName("PascalCaseName")` and `.WithTags("FeatureArea")`.
This populates the OpenAPI spec correctly.

```csharp
.WithName("EnsureProfile")
.WithTags("Profile")
```

Current tags in use: `Diagnostics`, `Profile`, `Strava`, `Athlete`, `Training`, `Activities`.
