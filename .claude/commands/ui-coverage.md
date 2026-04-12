# /ui-coverage — Report UI component, hook, and API client test coverage gaps

**Usage:** `/ui-coverage`

---

## Step 1 — Component inventory

Scan `ui/src/` for all React components — any `.tsx` file that exports a React component
(default or named export returning JSX). Exclude `main.tsx` and test setup files.

Group by feature folder:

| Folder | Examples |
|---|---|
| `ui/src/auth/` | `AuthProvider.tsx` |
| `ui/src/pages/` | `LoginPage.tsx`, `HomePage.tsx`, `StravaConnectedPage.tsx` |
| `ui/src/components/` | Shared UI components (if present) |
| `ui/src/features/dashboard/` | Dashboard-specific components (if present) |
| `ui/src/features/activities/` | Activity-related components (if present) |
| `ui/src/features/planner/` | Planner components (if present) |

For each component file, record:
- File path
- Component name(s) exported
- Whether it calls `useAuth()` (auth-gated rendering)
- Whether it uses any React Query hook from `marathonApi.ts` (data-fetching)

---

## Step 2 — Vitest coverage

For each component, check whether a corresponding `*.test.tsx` file exists in the same
folder or a `__tests__/` subfolder:

```bash
find ui/src -name "*.test.tsx"
```

For test files that exist, read them and count:
- Number of `describe` blocks
- Number of `it` / `test` cases

Check that the following interaction types are tested where applicable:

| Interaction type | Look for |
|---|---|
| User events | `fireEvent.*` or `userEvent.*` calls |
| Loading states | Assertions on loading text / skeleton / spinner |
| Error states | Assertions on error message rendering |
| Auth-gated rendering | `useAuth` mocked with `isAuthenticated: false` |

---

## Step 3 — Storybook coverage

For each component, check whether a corresponding `*.stories.tsx` file exists:

```bash
find ui/src -name "*.stories.tsx"
```

For story files that exist, read them and list the exported `const` names (each is a story).
Note whether the following story variants exist where applicable:

| Variant | Look for |
|---|---|
| Default | `export const Default` |
| Loading | `export const Loading` |
| Empty | `export const Empty` |
| Error | `export const Error` or `export const WithError` |
| Mobile | Story with `parameters.viewport` set |

---

## Step 4 — Hook coverage

Scan `ui/src/` for all custom hooks — files matching `use*.ts` or `use*.tsx`:

```bash
find ui/src -name "use*.ts" -o -name "use*.tsx"
```

For each hook, check for a corresponding `*.test.ts` or `*.test.tsx` file.

Key hooks that must be tested:

| Hook | Why it must be tested |
|---|---|
| `useAuth` | Central auth contract — if it breaks, all pages break |
| Any `useQuery` wrapper in `marathonApi.ts` | Exercises API client + React Query integration |
| Any `useMutation` wrapper in `marathonApi.ts` | Exercises write paths + optimistic updates |

For test files that exist, count `it` / `test` cases.

---

## Step 5 — API client coverage

Read `ui/src/api/marathonApi.ts` and list all exported functions (queries and mutations).

For each function, check whether it is exercised by at least one component test or hook test
via mock — search for the function name in `*.test.tsx` / `*.test.ts` files:

```bash
grep -r "{functionName}" ui/src --include="*.test.*"
```

---

## Step 6 — Output the coverage report

Save to `docs/coverage/ui-coverage.md` and print to console.

```markdown
# UI test coverage report
Generated: {date}

## Component coverage

| Component | Feature | Vitest exists | Test count | Stories exist | Story variants | Status |
|---|---|---|---|---|---|---|
| LoginPage | pages | YES | 3 | NO | — | PARTIAL |
| HomePage | pages | YES | 5 | NO | — | PARTIAL |
| StravaConnectedPage | pages | NO | — | NO | — | MISSING |
| AuthProvider | auth | NO | — | NO | — | MISSING |

Status: COVERED (Vitest test + story both exist), PARTIAL (one of two), MISSING (neither)

## Hook coverage

| Hook | Test exists | Test count | Key gaps |
|---|---|---|---|
| useAuth | NO | — | No test for token refresh, auth state transitions |
| useStravaStatus | NO | — | No test for loading / error states |

## API client coverage

| Function | Exercised in tests | Notes |
|---|---|---|
| useStravaStatus | NO | Not mocked in any component test |
| useEnsureProfile | NO | Not mocked in any component test |
| useConnectStrava | NO | Not mocked in any component test |

## Storybook inventory

List each .stories.tsx file and its exported story names.
(None found — Storybook not yet set up.)

## Summary

- Total components: N
- Fully covered (test + story): N (N%)
- Partial (one of two): N (N%)
- Missing (neither): N (N%)
- Total hooks: N | Covered: N (N%)
- Total API functions: N | Exercised in tests: N (N%)

## Recommended next tests (priority order)

1. **useAuth** — auth contract; all components depend on it; mock `useMsal` and test
   `isAuthenticated`, `getAccessToken()` token renewal, and `InteractionRequiredAuthError`
   fallback
2. **HomePage** — primary post-login view; test loading state, Strava connected vs.
   disconnected rendering, and error state from `useStravaStatus`
3. **useStravaStatus** — data-fetching hook; test loading, success, and error states with
   a mocked `apiRequest`
4. **StravaConnectedPage** — post-OAuth callback page; test that it triggers the
   profile mutation and handles failure
5. **AuthProvider** — wraps the whole app; test that it renders children and that
   `MsalProvider` receives the correct instance
```
