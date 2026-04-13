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
import { useEnsureProfile, useStravaStatus, useStravaAuthorise, useSyncActivities, useActivities, useTrainingLoad, useWeekSummary } from './marathonApi'

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

// ── useSyncActivities ─────────────────────────────────────────────────────────

describe('useSyncActivities', () => {
  beforeEach(() => {
    mockAuth()
    mockFetch.mockReset()
  })

  it('POSTs to /api/activities/sync and returns the sync result', async () => {
    const syncResult = { activitiesSynced: 5, activitiesSkipped: 2, syncedAt: '2026-04-12T10:00:00Z' }
    mockFetchOk(syncResult)

    const { result } = renderHook(() => useSyncActivities(), {
      wrapper: makeWrapper(),
    })

    result.current.mutate()

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/activities/sync'),
      expect.objectContaining({ method: 'POST' }),
    )
    expect(result.current.data).toEqual(syncResult)
  })

  it('throws when the API returns an error status', async () => {
    mockFetchError(422)

    const { result } = renderHook(() => useSyncActivities(), {
      wrapper: makeWrapper(),
    })

    result.current.mutate()

    await waitFor(() => expect(result.current.isError).toBe(true))
    expect(result.current.error).toBeInstanceOf(Error)
  })
})

// ── useActivities ─────────────────────────────────────────────────────────────

describe('useActivities', () => {
  beforeEach(() => {
    mockAuth()
    mockFetch.mockReset()
  })

  it('GETs /api/activities and returns the activity list', async () => {
    const listResult = {
      items: [
        { id: '1', name: 'Morning Run', activityType: 'Run', startedAt: '', durationSeconds: 3600, distanceMetres: 10000, tssScore: 55, averageHeartRateBpm: 145 },
      ],
      totalCount: 1, page: 1, pageSize: 10, totalPages: 1, hasNextPage: false, hasPreviousPage: false,
    }
    mockFetchOk(listResult)

    const { result } = renderHook(() => useActivities(), {
      wrapper: makeWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/activities'),
      expect.any(Object),
    )
    expect(result.current.data?.items).toHaveLength(1)
    expect(result.current.data?.totalCount).toBe(1)
  })

  it('appends type and pageSize query params when provided', async () => {
    mockFetchOk({ items: [], totalCount: 0, page: 1, pageSize: 5, totalPages: 0, hasNextPage: false, hasPreviousPage: false })

    const { result } = renderHook(() => useActivities({ type: 'Run', pageSize: 5 }), {
      wrapper: makeWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    const [url] = mockFetch.mock.calls[0] as [string]
    expect(url).toContain('type=Run')
    expect(url).toContain('pageSize=5')
  })
})

// ── useTrainingLoad ───────────────────────────────────────────────────────────

describe('useTrainingLoad', () => {
  beforeEach(() => {
    mockAuth()
    mockFetch.mockReset()
  })

  it('GETs /api/training/load with from and to params', async () => {
    const loadData = [
      { date: '2026-04-10', atl: 52.1, ctl: 47.3, tsb: -4.8, dailyTss: 85, isOvertrainingWarning: false, isOvertrainingDanger: false, isRaceReady: false, formDescription: 'Productive' },
    ]
    mockFetchOk(loadData)

    const { result } = renderHook(() => useTrainingLoad('2026-03-17', '2026-04-13'), {
      wrapper: makeWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    const [url] = mockFetch.mock.calls[0] as [string]
    expect(url).toContain('/api/training/load')
    expect(url).toContain('from=2026-03-17')
    expect(url).toContain('to=2026-04-13')
    expect(result.current.data).toHaveLength(1)
  })

  it('does not fetch when from or to are empty', () => {
    const { result } = renderHook(() => useTrainingLoad('', ''), {
      wrapper: makeWrapper(),
    })

    expect(result.current.fetchStatus).toBe('idle')
    expect(mockFetch).not.toHaveBeenCalled()
  })
})

// ── useWeekSummary ────────────────────────────────────────────────────────────

describe('useWeekSummary', () => {
  beforeEach(() => {
    mockAuth()
    mockFetch.mockReset()
  })

  it('GETs /api/training/week/{weekStart} and returns summary', async () => {
    const summary = {
      weekStart: '2026-04-07',
      totalTss: 320,
      runCount: 3, rideCount: 1, strengthCount: 2,
      runTss: 210, rideTss: 80, strengthTss: 30,
      trainingLoad: { date: '2026-04-13', atl: 50, ctl: 45, tsb: -5, dailyTss: 0, isOvertrainingWarning: false, isOvertrainingDanger: false, isRaceReady: false, formDescription: 'Productive' },
      hasOvertrainingWarning: false,
      recommendation: 'On track',
    }
    mockFetchOk(summary)

    const { result } = renderHook(() => useWeekSummary('2026-04-07'), {
      wrapper: makeWrapper(),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/training/week/2026-04-07'),
      expect.any(Object),
    )
    expect(result.current.data?.totalTss).toBe(320)
    expect(result.current.data?.recommendation).toBe('On track')
  })

  it('does not fetch when weekStart is empty', () => {
    const { result } = renderHook(() => useWeekSummary(''), {
      wrapper: makeWrapper(),
    })

    expect(result.current.fetchStatus).toBe('idle')
    expect(mockFetch).not.toHaveBeenCalled()
  })
})
