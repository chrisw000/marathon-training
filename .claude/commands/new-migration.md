# /new-migration — Generate an EF Core migration

**Usage:** `/new-migration`

The command will ask what schema change is needed, then generate and review the migration.
It does **not** apply the migration to any database.

---

Before starting, read `.claude/skills/ef-migrations.md` in full.

---

## Step 1 — Clarify the schema change

Describe the change needed:
- What new table(s) or column(s) are being added?
- What existing columns are being modified or removed?
- Are there any new indexes or foreign keys?

If the change requires modifying a domain aggregate or adding a `DbSet`, do that first and
confirm the `AppDbContext.OnModelCreating` configuration is correct before generating the
migration.

---

## Step 2 — Name the migration

Apply the naming convention from `ef-migrations.md`:  
`{PascalCasePurpose}_{YYYYMMDD}`

Examples: `AddTrainingWeek_20260501`, `AddActivityTssScore_20260601`

---

## Step 3 — Generate the migration

Run from the repository root (`marathon.trainer/`):

```bash
dotnet ef migrations add {MigrationName} \
  --project api/src/MarathonTraining.Infrastructure \
  --startup-project api/src/MarathonTraining.Api \
  --output-dir Migrations
```

This generates two files in `api/src/MarathonTraining.Infrastructure/Migrations/`:
- `{timestamp}_{MigrationName}.cs` — `Up()` and `Down()` methods
- `{timestamp}_{MigrationName}.Designer.cs` — snapshot metadata

---

## Step 4 — Review the generated migration

Read the generated migration file. Check for:

| Check | What to look for |
|---|---|
| Destructive operations | `DropColumn`, `DropTable`, `DropIndex` — flag these explicitly |
| Missing columns | All new properties in `OnModelCreating` should appear in `Up()` |
| Data type correctness | `nvarchar(450)` for string keys, `datetimeoffset` for `DateTimeOffset` |
| Index coverage | Unique indexes present where `IsUnique()` is configured |
| FK direction | FK column is on the dependent entity, not the principal |
| Reversibility | `Down()` correctly undoes everything in `Up()` |

Report the review findings as a checklist with PASS / WARN / FAIL per item.

**Flag WARN or FAIL items to the user before proceeding.** Do not auto-fix silently.

---

## Step 5 — Update ef-migrations.md

Add the new migration to the "Current migrations" list in `.claude/skills/ef-migrations.md`:

```markdown
| `{MigrationName}` | {YYYY-MM-DD} | {One-line description of what it does} |
```

---

## Step 6 — Stop here

Do **not** run `dotnet ef database update` or call `MigrateAsync`. The migration is staged
for review only. The user applies it when ready.

Remind the user:
- Apply with: `dotnet ef database update --project api/src/MarathonTraining.Infrastructure --startup-project api/src/MarathonTraining.Api`
- In Development, `EnsureCreatedAsync` does not apply migrations — switch to `MigrateAsync` in `Program.cs` once migrations are in use
