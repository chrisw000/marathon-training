# EF Core / Database Conventions

---

## DbContext

**Class:** `AppDbContext`  
**Location:** `api/src/MarathonTraining.Infrastructure/Persistence/AppDbContext.cs`  
**Registered in:** `Program.cs` — `builder.Services.AddDbContext<AppDbContext>(...)`  
**Connection string key:** `ConnectionStrings:DefaultConnection`

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();
    public DbSet<StravaConnection> StravaConnections => Set<StravaConnection>();
}
```

All model configuration is in `OnModelCreating` — no EF data annotations on domain classes.

---

## Schema management — current state

**Migrations are in use.** In Development, `EnsureCreatedAsync` is called at startup:

```csharp
// Program.cs (Development only)
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.EnsureCreatedAsync();
```

`EnsureCreatedAsync` creates the schema if it does not exist and is a no-op otherwise.
It does **not** apply incremental changes — use migrations when the schema needs to evolve.

**When to switch to migrations:** Before any non-Development deployment or when the schema
needs to change without dropping and recreating the database.

---

## How to add a migration

The Infrastructure project contains the DbContext and must be the `--project` target.
The Api project is the startup project (has the connection string) and must be `--startup-project`.

```bash
dotnet ef migrations add <MigrationName> \
  --project api/src/MarathonTraining.Infrastructure \
  --startup-project api/src/MarathonTraining.Api \
  --output-dir Migrations
```

Run from the repository root (`marathon.trainer/`).

---

## Migration naming convention

`{PascalCasePurpose}_{YYYYMMDD}`

Examples:
- `InitialSchema_20260412`
- `AddTrainingWeek_20260501`
- `AddActivityTssScore_20260601`

The date suffix makes the migration list self-documenting and avoids name collisions.

---

## How to apply migrations locally

```bash
dotnet ef database update \
  --project api/src/MarathonTraining.Infrastructure \
  --startup-project api/src/MarathonTraining.Api
```

Or just restart the API in Development — `EnsureCreatedAsync` handles schema creation for the
initial setup. Once migrations are in use, switch to `MigrateAsync` instead:

```csharp
await db.Database.MigrateAsync();  // applies all pending migrations
```

---

## Current migrations

| Migration | Date | Purpose |
|---|---|---|
| `AddAthletePhysiology_20260412` | 2026-04-12 | Initial schema — creates `AthleteProfiles` (including `RestingHr`, `MaxHr`, `ThresholdHr`, `FtpWatts`, `CurrentPhase`, `LastSyncedAt`) and `StravaConnections` tables |
| `AddActivityStravaFields_20260412` | 2026-04-13 | Adds Strava-specific columns to `Activities` (`StravaActivityId`, `StravaActivityType`, `HasHeartRate`, `AveragePowerWatts`, `IsDevicePower`, `AverageSpeedMetresPerSecond`, `ExternalSource`) and a filtered unique index on `StravaActivityId` |

---

## Index conventions (from OnModelCreating)

```csharp
// AthleteProfiles — UserId must be unique (one profile per Entra user)
entity.HasIndex(e => e.UserId).IsUnique();

// StravaConnections — AthleteProfileId is both PK and FK (one connection per athlete)
entity.HasKey(e => e.AthleteProfileId);
entity.HasOne<AthleteProfile>()
      .WithOne(a => a.StravaConnection)
      .HasForeignKey<StravaConnection>(e => e.AthleteProfileId);
```

All indexes are configured in `OnModelCreating`, not as EF attributes on the entity.

---

## Seed data

No seed data is defined. The `EnsureAthleteProfileCommand` creates the first row on first
login. All other data enters through normal application flows.

---

## EF Design-time factory

Not yet needed — `Program.cs` is the startup project and EF CLI can resolve the `AppDbContext`
from it. If a design-time factory is ever needed (e.g. for CI migrations), create
`AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` in the Infrastructure project.
