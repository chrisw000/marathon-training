# UI Patterns

Applies to: `ui/src/`  
Stack: React 19, TypeScript ~6, Vite 8, pnpm 10

---

## MSAL auth

**Configuration:** `ui/src/auth/msalConfig.ts`

```typescript
const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority: import.meta.env.VITE_ENTRA_AUTHORITY,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: { cacheLocation: 'sessionStorage' },  // tab-scoped, safer than localStorage
};

export const apiScopes: string[] = [import.meta.env.VITE_API_SCOPE];
export const msalInstance = new PublicClientApplication(msalConfig);
```

**Provider:** `ui/src/auth/AuthProvider.tsx` wraps children in `<MsalProvider instance={msalInstance}>`.
Mounted once in `main.tsx` alongside `<QueryClientProvider>`.

---

## useAuth hook

`ui/src/auth/useAuth.ts` — the single interface for auth operations across the app.

```typescript
interface UseAuthResult {
  isAuthenticated: boolean;
  user: { name: string; email: string } | null;
  login: () => Promise<void>;       // loginRedirect with apiScopes
  logout: () => Promise<void>;      // logoutRedirect
  getAccessToken: () => Promise<string>; // acquireTokenSilent → acquireTokenRedirect fallback
}
```

`getAccessToken()` uses silent renewal; if `InteractionRequiredAuthError` is thrown it falls
back to `acquireTokenRedirect` (redirect-based interactive renewal). It never throws silently —
callers can `await` it safely.

Do not call `useMsal()` or `useIsAuthenticated()` directly in components. Always go through
`useAuth()`.

---

## API client pattern

`ui/src/api/marathonApi.ts` — all API hooks live here.

**Base request helper:**
```typescript
const API_BASE = import.meta.env.VITE_API_BASE_URL;  // e.g. http://localhost:5259

async function apiRequest<T>(
  path: string,
  token: string,
  options?: RequestInit,
): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });
  if (!response.ok) throw new Error(`API request failed: ${response.status} ${response.statusText}`);
  return response.json() as Promise<T>;
}
```

The Bearer token is always fetched fresh via `getAccessToken()` before each request —
MSAL's silent renewal keeps it valid.

---

## React Query usage

**Queries** (GET, data fetching):
```typescript
export function useStravaStatus() {
  const { getAccessToken } = useAuth();
  return useQuery({
    queryKey: ['strava-status'],
    queryFn: async (): Promise<StravaStatus> => {
      const token = await getAccessToken();
      return apiRequest('/api/strava/status', token);
    },
  });
}
```

**Mutations** (POST/DELETE, actions):
```typescript
export function useEnsureProfile() {
  const { getAccessToken } = useAuth();
  return useMutation({
    mutationFn: async (): Promise<EnsureProfileResult> => {
      const token = await getAccessToken();
      return apiRequest('/api/profile', token, { method: 'POST' });
    },
  });
}
```

**Side effects on success** (e.g. navigate to URL):
```typescript
return useMutation({
  mutationFn: ...,
  onSuccess: ({ url }) => { window.location.href = url; },
});
```

**Triggering refetch after a mutation:**
```typescript
ensureProfile.mutate(undefined, {
  onSuccess: () => { void stravaStatus.refetch(); },
});
```

---

## Component folder structure

```
ui/src/
├── auth/              # Auth plumbing only — no UI components here
│   ├── msalConfig.ts
│   ├── AuthProvider.tsx
│   └── useAuth.ts
├── api/               # React Query hooks — no fetch calls outside this folder
│   └── marathonApi.ts
├── pages/             # Route-level components (one file per route)
│   ├── LoginPage.tsx
│   ├── HomePage.tsx
│   └── StravaConnectedPage.tsx
├── App.tsx            # Router + ProtectedRoute
└── main.tsx           # Entry point
```

When adding shared UI components, create `ui/src/components/`. When adding domain-specific
hooks that aren't API calls, create `ui/src/hooks/`.

---

## Protected routes

```typescript
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useIsAuthenticated();
  return isAuthenticated ? <>{children}</> : <Navigate to="/" replace />;
}
```

Wrap any page that requires authentication in `<ProtectedRoute>`.

The root route `/` redirects authenticated users to `/home` and shows `<LoginPage>` otherwise.

---

## Current routes

| Path | Component | Auth |
|---|---|---|
| `/` | `RootRoute` → `LoginPage` or redirect to `/home` | — |
| `/home` | `HomePage` | Required |
| `/strava-connected` | `StravaConnectedPage` | Required |

---

## Environment variables

All in `ui/.env.local` (gitignored). See `ui/.env.example` for the full list with comments.

| Variable | Description |
|---|---|
| `VITE_ENTRA_CLIENT_ID` | SPA app registration client ID in Entra External ID |
| `VITE_ENTRA_AUTHORITY` | `https://<tenant>.ciamlogin.com/<tenant>.onmicrosoft.com` |
| `VITE_API_SCOPE` | `api://<api-client-id>/<scope-name>` |
| `VITE_API_BASE_URL` | API base URL — `http://localhost:5259` for local dev |
| `VITE_STRAVA_REDIRECT_URI` | Must match Strava API settings and API config |

`VITE_*` variables are inlined at build time. Never store secrets in them.

---

## UI tests

**Framework:** Vitest + @testing-library/react  
**Setup:** `ui/src/test/setup.ts` (imports `@testing-library/jest-dom`)  
**Run:** `pnpm test` (watch) or `pnpm test:run` (CI)

Test files are co-located with the component they test: `HomePage.test.tsx` next to `HomePage.tsx`.
