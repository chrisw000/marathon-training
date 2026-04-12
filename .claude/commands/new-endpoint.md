# /new-endpoint — Add a single minimal API endpoint

**Usage:** `/new-endpoint <Route> <Method> <HandlerName> <Auth:y/n>`  
Example: `/new-endpoint /api/training-weeks GET GetTrainingWeeks y`

---

Before writing anything, read `.claude/skills/api-endpoints.md` in full.

---

## Inputs

| Parameter | Description | Example |
|---|---|---|
| Route | Full route path, lowercase kebab-case | `/api/training-weeks` |
| Method | HTTP verb | `GET`, `POST`, `DELETE` |
| HandlerName | MediatR command or query to dispatch | `GetTrainingWeeksQuery` |
| Auth | Whether the endpoint requires authentication | `y` or `n` |

---

## Step 1 — Verify the handler exists

Check that `{HandlerName}` (command or query) and its handler class exist in
`api/src/MarathonTraining.Application/`. If not, stop and run `/new-feature` first —
this command only wires up an existing handler to a new HTTP route.

---

## Step 2 — Add the endpoint to Program.cs

Add the endpoint in `api/src/MarathonTraining.Api/Program.cs` in the correct feature group
(separated by the `// ── {Feature} ───` comment blocks):

**Authenticated endpoint:**
```csharp
app.Map{Method}("{route}", async (ClaimsPrincipal user, ISender sender) =>
{
    var userId = user.GetObjectId()
        ?? throw new InvalidOperationException("No object ID claim found on the authenticated user.");
    var result = await sender.Send(new {HandlerName}(userId));
    return Results.Ok(result);
})
.WithName("{EndpointName}")
.WithTags("{FeatureTag}");
```

**Anonymous endpoint** (add `.AllowAnonymous()`):
```csharp
app.Map{Method}("{route}", async (ISender sender, ...) =>
{
    ...
})
.AllowAnonymous()
.WithName("{EndpointName}")
.WithTags("{FeatureTag}");
```

---

## Step 3 — Add a unit test for the handler

If no unit test exists for `{HandlerName}`, create one in
`api/tests/MarathonTraining.Application.Tests/` following the naming conventions in
`testing-patterns.md`. Cover at minimum the happy path.

---

## Step 4 — Update the endpoint reference table

Open `.claude/skills/api-endpoints.md` and add a row to the endpoint reference table:

```
| {Method} | {Route} | {Required/Anonymous} | {HandlerName} — {description} |
```

---

## Step 5 — Build and verify

```bash
dotnet build api/src/MarathonTraining.Api/
dotnet test api/tests/MarathonTraining.Application.Tests/
```

Fix any errors before continuing.
