import { describe, it, expect, vi } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { ActivitiesPage } from './ActivitiesPage'

vi.mock('../auth/useAuth', () => ({
  useAuth: () => ({
    getAccessToken: vi.fn().mockResolvedValue('test-token'),
  }),
}))

vi.mock('../api/marathonApi', () => ({
  useActivities: vi.fn(),
  useSyncActivities: vi.fn(),
  useLogManualActivity: vi.fn(),
}))

import { useActivities, useSyncActivities, useLogManualActivity } from '../api/marathonApi'

const baseActivity = {
  id: '1',
  name: 'Morning Run',
  activityType: 'Run',
  startedAt: '2026-04-10T07:00:00Z',
  durationSeconds: 3600,
  distanceMetres: 10000,
  tssScore: 65,
  averageHeartRateBpm: 148,
}

const baseListResult = {
  items: [baseActivity],
  totalCount: 1,
  page: 1,
  pageSize: 20,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
}

function setupDefaultMocks() {
  vi.mocked(useActivities).mockReturnValue({
    isLoading: false,
    isError: false,
    data: baseListResult,
    refetch: vi.fn(),
  } as any)
  vi.mocked(useSyncActivities).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    isSuccess: false,
    isError: false,
    data: undefined,
  } as any)
  vi.mocked(useLogManualActivity).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    isSuccess: false,
    isError: false,
    data: undefined,
  } as any)
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ActivitiesPage />
    </MemoryRouter>,
  )
}

describe('ActivitiesPage', () => {
  it('shows loading state', () => {
    vi.mocked(useActivities).mockReturnValue({ isLoading: true, isError: false, data: undefined, refetch: vi.fn() } as any)
    vi.mocked(useSyncActivities).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)
    vi.mocked(useLogManualActivity).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)

    renderPage()
    expect(screen.getByText(/loading activities/i)).toBeInTheDocument()
  })

  it('shows error state when activities fail to load', () => {
    vi.mocked(useActivities).mockReturnValue({ isLoading: false, isError: true, data: undefined, refetch: vi.fn() } as any)
    vi.mocked(useSyncActivities).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)
    vi.mocked(useLogManualActivity).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)

    renderPage()
    expect(screen.getByText(/failed to load activities/i)).toBeInTheDocument()
  })

  it('shows empty state message when list is empty', () => {
    vi.mocked(useActivities).mockReturnValue({
      isLoading: false, isError: false, refetch: vi.fn(),
      data: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNextPage: false, hasPreviousPage: false },
    } as any)
    vi.mocked(useSyncActivities).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)
    vi.mocked(useLogManualActivity).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)

    renderPage()
    expect(screen.getByText(/no activities found/i)).toBeInTheDocument()
  })

  it('renders activity rows', () => {
    setupDefaultMocks()
    renderPage()
    const tbody = screen.getByRole('table').querySelector('tbody')!
    expect(within(tbody).getByText('Morning Run')).toBeInTheDocument()
    expect(within(tbody).getByText('Run')).toBeInTheDocument()
    expect(within(tbody).getByText('1h 0m')).toBeInTheDocument()
    expect(within(tbody).getByText('10.0 km')).toBeInTheDocument()
    expect(within(tbody).getByText('65')).toBeInTheDocument()
  })

  it('shows — for null distance and null TSS', () => {
    vi.mocked(useActivities).mockReturnValue({
      isLoading: false, isError: false, refetch: vi.fn(),
      data: {
        items: [{ ...baseActivity, distanceMetres: null, tssScore: null }],
        totalCount: 1, page: 1, pageSize: 20, totalPages: 1, hasNextPage: false, hasPreviousPage: false,
      },
    } as any)
    vi.mocked(useSyncActivities).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)
    vi.mocked(useLogManualActivity).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, data: undefined } as any)

    renderPage()
    expect(screen.getAllByText('—')).toHaveLength(2)
  })

  it('calls sync mutate when Sync button is clicked', async () => {
    const mockSyncMutate = vi.fn()
    setupDefaultMocks()
    vi.mocked(useSyncActivities).mockReturnValue({
      mutate: mockSyncMutate, isPending: false, isSuccess: false, isError: false, data: undefined,
    } as any)

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /sync from strava/i }))
    expect(mockSyncMutate).toHaveBeenCalled()
  })

  it('shows sync result after successful sync', () => {
    setupDefaultMocks()
    vi.mocked(useSyncActivities).mockReturnValue({
      mutate: vi.fn(), isPending: false, isSuccess: true, isError: false,
      data: { activitiesSynced: 3, activitiesSkipped: 1, syncedAt: '2026-04-13T10:00:00Z' },
    } as any)

    renderPage()
    expect(screen.getByText(/synced 3 activities/i)).toBeInTheDocument()
  })

  it('shows error when sync fails', () => {
    setupDefaultMocks()
    vi.mocked(useSyncActivities).mockReturnValue({
      mutate: vi.fn(), isPending: false, isSuccess: false, isError: true, data: undefined,
    } as any)

    renderPage()
    expect(screen.getByText(/sync failed/i)).toBeInTheDocument()
  })

  it('calls logActivity mutate when log form is submitted', async () => {
    const mockLogMutate = vi.fn()
    setupDefaultMocks()
    vi.mocked(useLogManualActivity).mockReturnValue({
      mutate: mockLogMutate, isPending: false, isSuccess: false, isError: false, data: undefined,
    } as any)

    renderPage()
    await userEvent.type(screen.getByLabelText(/session name/i), 'Gym — legs')
    await userEvent.type(screen.getByLabelText(/date and time/i), '2026-04-13T09:00')
    await userEvent.type(screen.getByLabelText(/duration/i), '45')
    await userEvent.type(screen.getByLabelText(/rpe/i), '7')
    await userEvent.click(screen.getByRole('button', { name: /log session/i }))

    expect(mockLogMutate).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Gym — legs',
        activityType: 'Strength',
        durationMinutes: 45,
        rpe: 7,
      }),
      expect.any(Object),
    )
  })

  it('shows TSS after session is logged', () => {
    setupDefaultMocks()
    vi.mocked(useLogManualActivity).mockReturnValue({
      mutate: vi.fn(), isPending: false, isSuccess: true, isError: false,
      data: { activityId: 'abc', tssScore: 42 },
    } as any)

    renderPage()
    expect(screen.getByText(/session logged/i)).toBeInTheDocument()
    expect(screen.getByText(/tss: 42/i)).toBeInTheDocument()
  })

  it('shows error when log fails', () => {
    setupDefaultMocks()
    vi.mocked(useLogManualActivity).mockReturnValue({
      mutate: vi.fn(), isPending: false, isSuccess: false, isError: true, data: undefined,
    } as any)

    renderPage()
    expect(screen.getByText(/failed to log session/i)).toBeInTheDocument()
  })

  it('disables log button while saving', () => {
    setupDefaultMocks()
    vi.mocked(useLogManualActivity).mockReturnValue({
      mutate: vi.fn(), isPending: true, isSuccess: false, isError: false, data: undefined,
    } as any)

    renderPage()
    expect(screen.getByRole('button', { name: /saving/i })).toBeDisabled()
  })
})
