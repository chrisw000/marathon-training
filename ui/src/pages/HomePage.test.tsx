import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
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
}))

import { useEnsureProfile, useStravaStatus, useStravaAuthorise } from '../api/marathonApi'

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
}

describe('HomePage', () => {
  it('renders the user display name', () => {
    setupDefaultMocks()
    render(<HomePage />)
    expect(screen.getByText(/Ada Lovelace/)).toBeInTheDocument()
  })

  it('renders a sign-out button', () => {
    setupDefaultMocks()
    render(<HomePage />)
    expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
  })

  it('shows a loading message while Strava status is loading', () => {
    vi.mocked(useEnsureProfile).mockReturnValue({ mutate: mockMutate, isPending: false } as any)
    vi.mocked(useStravaStatus).mockReturnValue({ isLoading: true, data: undefined, refetch: vi.fn() } as any)
    vi.mocked(useStravaAuthorise).mockReturnValue({ mutate: vi.fn(), isPending: false } as any)

    render(<HomePage />)
    expect(screen.getByText(/checking strava connection/i)).toBeInTheDocument()
  })

  it('shows connected message when Strava is connected', () => {
    vi.mocked(useEnsureProfile).mockReturnValue({ mutate: mockMutate, isPending: false } as any)
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: true, stravaAthleteId: 12345, expiresAt: null },
      refetch: vi.fn(),
    } as any)
    vi.mocked(useStravaAuthorise).mockReturnValue({ mutate: vi.fn(), isPending: false } as any)

    render(<HomePage />)
    expect(screen.getByText(/strava connected/i)).toBeInTheDocument()
  })

  it('shows connect button when Strava is not connected', () => {
    vi.mocked(useEnsureProfile).mockReturnValue({ mutate: mockMutate, isPending: false } as any)
    vi.mocked(useStravaStatus).mockReturnValue({
      isLoading: false,
      data: { isConnected: false, stravaAthleteId: null, expiresAt: null },
      refetch: vi.fn(),
    } as any)
    vi.mocked(useStravaAuthorise).mockReturnValue({ mutate: vi.fn(), isPending: false } as any)

    render(<HomePage />)
    expect(screen.getByRole('button', { name: /connect strava/i })).toBeInTheDocument()
  })
})
