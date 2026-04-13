import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement } from 'react'

// ── Module mocks ──────────────────────────────────────────────────────────────

vi.mock('@azure/msal-browser', () => ({
  PublicClientApplication: vi.fn(function () {}),
  InteractionRequiredAuthError: class extends Error {},
}))

vi.mock('../auth/useAuth', () => ({
  useAuth: vi.fn(),
}))

// Stub fetch globally — tests control what each endpoint returns.
const mockFetch = vi.fn()
vi.stubGlobal('fetch', mockFetch)

// Stub window.location.href — useStravaAuthorise redirects to the Strava URL.
Object.defineProperty(window, 'location', {
  value: { href: '' },
  writable: true,
})

import { useAuth } from '../auth/useAuth'
import { useEnsureProfile, useStravaStatus, useStravaAuthorise } from './marathonApi'

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return ({ children }: { children: React.ReactNode }) =>
    createElement(QueryClientProvider, { client }, children)
}

function mockAuth(token = 'test-access-token') {
  vi.mocked(useAuth).mockReturnValue({
    getAccessToken: vi.fn().mockResolvedValue(token),
    isAuthenticated: true,
    user: { name: 'Test User', email: 'test@example.com' },
    login: vi.fn(),
    logout: vi.fn(),
  })
}

function mockFetchOk(body: unknown) {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    json: () => Promise.resolve(body),
    status: 200,
    statusText: 'OK',
  })
}

function mockFetchError(status: number) {
  mockFetch.mockResolvedValueOnce({
    ok: false,
    json: () => Promise.resolve({}),
    status,
    statusText: 'Error',
  })
}

// ── useEnsureProfile ──────────────────────────────────────────────────────────

describe('useEnsureProfile', () => {
  beforeEach(() => {
    mockAuth()
    mockFetch.mockReset()
  })

  it('POSTs to /api/profile and returns the result', async () => {
    mockFetchOk({ created: true })

    const { result } = renderHook(() => useEnsureProfile(), {
      wrapper: makeWrapper(),
    })

    result.current.mutate()

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/profile'),
      expect.objectContaining({ method: 'POST' }),
    )
    expect(result.current.data).toEqual({ created: true })
  })

  it('includes Authorization header with Bearer token', async () => {
    mockFetchOk({ created: false })

    const { result } = renderHook(() => useEnsureProfile(), {
      wrapper: makeWrapper(),
    })

    result.current.mutate()

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    const [, options] = mockFetch.mock.calls[0] as [string, RequestInit]
    expect((options.headers as Record<string, string>)['Authorization']).toBe(
      'Bearer test-access-token',
    )
  })

  it('throws when the API returns an error status', async () => {
    mockFetchError(500)

    const { result } = renderHook(() => useEnsureProfile(), {
      wrapper: makeWrapper(),
    })

    result.current.mutate()

    await waitFor(() => expect(result.current.isError).toBe(true))
    expect(result.current.error).toBeInstanceOf(Error)
  })
})

// ── useStravaStatus ───────────────────────────────────────────────────────────

describe('useStravaStatus', () => {
  beforeEach(() => {
    mockAuth()
    mockFetch.mockReset()
  })

  it('GETs /api/strava/status and returns connection data', async () => {
    const statusData = { isConnected: true, stravaAthleteId: 12345, expiresAt: null }
    mockFetchOk(statusData)

    const { result } = renderHook(() => useStravaStatus(), {
      wrapper: makeWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/strava/status'),
      expect.any(Object),
    )
    expect(result.current.data).toEqual(statusData)
  })

  it('returns not-connected shape when athlete has no Strava link', async () => {
    mockFetchOk({ isConnected: false, stravaAthleteId: null, expiresAt: null })

    const { result } = renderHook(() => useStravaStatus(), {
      wrapper: makeWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data?.isConnected).toBe(false)
  })
})

// ── useStravaAuthorise ────────────────────────────────────────────────────────

describe('useStravaAuthorise', () => {
  beforeEach(() => {
    mockAuth()
    mockFetch.mockReset()
    window.location.href = ''
  })

  it('GETs /api/strava/authorise and redirects to the returned URL', async () => {
    const stravaUrl = 'https://www.strava.com/oauth/authorize?client_id=123'
    mockFetchOk({ url: stravaUrl })

    const { result } = renderHook(() => useStravaAuthorise(), {
      wrapper: makeWrapper(),
    })

    result.current.mutate()

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/strava/authorise'),
      expect.any(Object),
    )
    expect(window.location.href).toBe(stravaUrl)
  })
})
