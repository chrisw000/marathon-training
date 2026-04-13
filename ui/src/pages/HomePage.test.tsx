import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { HomePage } from './HomePage'

vi.mock('../auth/useAuth', () => ({
  useAuth: () => ({
    user: { name: 'Ada Lovelace', email: 'ada@example.com' },
    logout: vi.fn(),
    getAccessToken: vi.fn().mockResolvedValue('test-token'),
  }),
}))

vi.mock('../api/marathonApi', () => ({
  useEnsureProfile: vi.fn(),
  useStravaStatus: vi.fn(),
  useStravaAuthorise: vi.fn(),
  useSyncActivities: vi.fn(),
  useActivities: vi.fn(),
}))

import {
  useEnsureProfile,
  useStravaStatus,
  useStravaAuthorise,
  useSyncActivities,
  useActivities,
} from '../api/marathonApi'

const mockMutate = vi.fn()

function setupDefaultMocks() {
  vi.mocked(useEnsureProfile).mockReturnValue({
    mutate: mockMutate,
    isPending: false,
  } as any)

  vi.mocked(useStravaStatus).mockReturnValue({
    isLoading: false,
    data: undefined,
    refetch: vi.fn(),
  } as any)

  vi.mocked(useStravaAuthorise).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
  } as any)

  vi.mocked(useSyncActivities).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    isSuccess: false,
    isError: false,
    data: undefined,
  } as any)

  vi.mocked(useActivities).mockReturnValue({
    data: undefined,
    refetch: vi.fn(),
  } as any)
}

describe('HomePage', () => {
  it('renders the user display name', () => {
    setupDefaultMocks()
    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByText(/Ada Lovelace/)).toBeInTheDocument()
  })

  it('renders a sign-out button', () => {
    setupDefaultMocks()
    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
  })

  it('shows a loading message while Strava status is loading', () => {
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({ isLoading: true, data: undefined, refetch: vi.fn() } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByText(/checking strava connection/i)).toBeInTheDocument()
  })

  it('shows connected message and sync button when Strava is connected', () => {
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: true, stravaAthleteId: 12345, expiresAt: null },
      refetch: vi.fn(),
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByText(/strava connected/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sync activities/i })).toBeInTheDocument()
  })

  it('shows connect button when Strava is not connected', () => {
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: false, stravaAthleteId: null, expiresAt: null },
      refetch: vi.fn(),
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /connect strava/i })).toBeInTheDocument()
  })

  it('shows "Syncing…" while sync is pending', () => {
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: true, stravaAthleteId: 12345, expiresAt: null },
      refetch: vi.fn(),
    } as any)
    vi.mocked(useSyncActivities).mockReturnValue({
      mutate: vi.fn(),
      isPending: true,
      isSuccess: false,
      isError: false,
      data: undefined,
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /syncing/i })).toBeDisabled()
  })

  it('shows sync count after a successful sync', () => {
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: true, stravaAthleteId: 12345, expiresAt: null },
      refetch: vi.fn(),
    } as any)
    vi.mocked(useSyncActivities).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: true,
      isError: false,
      data: { activitiesSynced: 3, activitiesSkipped: 1, syncedAt: '2026-04-12T10:00:00Z' },
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByText(/3 activities synced/i)).toBeInTheDocument()
  })

  it('shows error message when sync fails', () => {
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: true, stravaAthleteId: 12345, expiresAt: null },
      refetch: vi.fn(),
    } as any)
    vi.mocked(useSyncActivities).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: false,
      isError: true,
      data: undefined,
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByText(/sync failed/i)).toBeInTheDocument()
  })

  it('renders a list of recent activities when data is available', () => {
    setupDefaultMocks()
    vi.mocked(useActivities).mockReturnValue({
      data: {
        items: [
          { id: '1', name: 'Morning Run', activityType: 'Run', startedAt: '', durationSeconds: 3600, distanceMetres: 10000, tssScore: 55, averageHeartRateBpm: 145 },
          { id: '2', name: 'Strength Session', activityType: 'Strength', startedAt: '', durationSeconds: 2700, distanceMetres: null, tssScore: 30, averageHeartRateBpm: null },
        ],
        totalCount: 2, page: 1, pageSize: 10, totalPages: 1, hasNextPage: false, hasPreviousPage: false,
      },
      refetch: vi.fn(),
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByText(/Morning Run/)).toBeInTheDocument()
    expect(screen.getByText(/Strength Session/)).toBeInTheDocument()
    expect(screen.getByText(/TSS 55/)).toBeInTheDocument()
  })

  it('calls sync mutate when sync button is clicked', async () => {
    const mockSync = vi.fn()
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: true, stravaAthleteId: 12345, expiresAt: null },
      refetch: vi.fn(),
    } as any)
    vi.mocked(useSyncActivities).mockReturnValue({
      mutate: mockSync,
      isPending: false,
      isSuccess: false,
      isError: false,
      data: undefined,
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    await userEvent.click(screen.getByRole('button', { name: /sync activities/i }))
    expect(mockSync).toHaveBeenCalledOnce()
  })

  it('shows dashboard link when Strava is connected', () => {
    setupDefaultMocks()
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: true, stravaAthleteId: 12345, expiresAt: null },
      refetch: vi.fn(),
    } as any)

    render(<MemoryRouter><HomePage /></MemoryRouter>)
    expect(screen.getByRole('link', { name: /view training dashboard/i })).toBeInTheDocument()
  })
})
