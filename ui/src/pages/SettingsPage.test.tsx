import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { SettingsPage } from './SettingsPage'

vi.mock('../auth/useAuth', () => ({
  useAuth: () => ({
    getAccessToken: vi.fn().mockResolvedValue('test-token'),
  }),
}))

vi.mock('../api/marathonApi', () => ({
  useAthleteProfile: vi.fn(),
  useUpdatePhysiology: vi.fn(),
  useUpdateTrainingPhase: vi.fn(),
}))

import { useAthleteProfile, useUpdatePhysiology, useUpdateTrainingPhase } from '../api/marathonApi'

const baseProfile = {
  id: 'abc',
  displayName: 'Chris W',
  restingHr: 48,
  maxHr: 178,
  thresholdHr: 160,
  ftpWatts: 220,
  currentPhase: 'Base',
  hasStravaConnected: true,
  lastSyncedAt: null,
}

function setupDefaultMocks() {
  vi.mocked(useAthleteProfile).mockReturnValue({
    isLoading: false,
    isError: false,
    data: baseProfile,
  } as any)
  vi.mocked(useUpdatePhysiology).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    isSuccess: false,
    isError: false,
  } as any)
  vi.mocked(useUpdateTrainingPhase).mockReturnValue({
    mutate: vi.fn(),
    isPending: false,
    isSuccess: false,
    isError: false,
  } as any)
}

function renderSettings() {
  return render(
    <MemoryRouter>
      <SettingsPage />
    </MemoryRouter>,
  )
}

describe('SettingsPage', () => {
  it('shows loading state', () => {
    vi.mocked(useAthleteProfile).mockReturnValue({ isLoading: true, isError: false, data: undefined } as any)
    vi.mocked(useUpdatePhysiology).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false } as any)
    vi.mocked(useUpdateTrainingPhase).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false } as any)

    renderSettings()
    expect(screen.getByText(/loading settings/i)).toBeInTheDocument()
  })

  it('shows error state when profile fails to load', () => {
    vi.mocked(useAthleteProfile).mockReturnValue({ isLoading: false, isError: true, data: undefined } as any)
    vi.mocked(useUpdatePhysiology).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false } as any)
    vi.mocked(useUpdateTrainingPhase).mockReturnValue({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false } as any)

    renderSettings()
    expect(screen.getByText(/unable to load profile/i)).toBeInTheDocument()
  })

  it('renders the athlete display name', () => {
    setupDefaultMocks()
    renderSettings()
    expect(screen.getByText('Chris W')).toBeInTheDocument()
  })

  it('pre-populates HR fields from profile data', () => {
    setupDefaultMocks()
    renderSettings()
    expect(screen.getByLabelText('Resting HR (bpm)')).toHaveValue(48)
    expect(screen.getByLabelText('Max HR (bpm)')).toHaveValue(178)
    expect(screen.getByLabelText(/threshold hr/i)).toHaveValue(160)
    expect(screen.getByLabelText(/ftp/i)).toHaveValue(220)
  })

  it('pre-selects the current training phase', () => {
    setupDefaultMocks()
    renderSettings()
    expect(screen.getByLabelText(/phase/i)).toHaveValue('Base')
  })

  it('calls updatePhase mutate when phase form is submitted', async () => {
    const mockMutate = vi.fn()
    setupDefaultMocks()
    vi.mocked(useUpdateTrainingPhase).mockReturnValue({
      mutate: mockMutate,
      isPending: false,
      isSuccess: false,
      isError: false,
    } as any)

    renderSettings()
    await userEvent.click(screen.getByRole('button', { name: /save phase/i }))
    expect(mockMutate).toHaveBeenCalledWith('Base')
  })

  it('calls updatePhysiology mutate when physiology form is submitted', async () => {
    const mockMutate = vi.fn()
    setupDefaultMocks()
    vi.mocked(useUpdatePhysiology).mockReturnValue({
      mutate: mockMutate,
      isPending: false,
      isSuccess: false,
      isError: false,
    } as any)

    renderSettings()
    await userEvent.click(screen.getByRole('button', { name: /save physiology/i }))
    expect(mockMutate).toHaveBeenCalledWith({
      restingHr: 48,
      maxHr: 178,
      thresholdHr: 160,
      ftpWatts: 220,
    })
  })

  it('shows success confirmation after phase saved', () => {
    setupDefaultMocks()
    vi.mocked(useUpdateTrainingPhase).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: true,
      isError: false,
    } as any)

    renderSettings()
    expect(screen.getByText(/phase saved/i)).toBeInTheDocument()
  })

  it('shows success confirmation after physiology saved', () => {
    setupDefaultMocks()
    vi.mocked(useUpdatePhysiology).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: true,
      isError: false,
    } as any)

    renderSettings()
    expect(screen.getByText(/physiology saved/i)).toBeInTheDocument()
  })

  it('shows error message when physiology save fails', () => {
    setupDefaultMocks()
    vi.mocked(useUpdatePhysiology).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
      isSuccess: false,
      isError: true,
    } as any)

    renderSettings()
    expect(screen.getByText(/failed to save/i)).toBeInTheDocument()
  })

  it('disables save buttons while saving', () => {
    setupDefaultMocks()
    vi.mocked(useUpdatePhysiology).mockReturnValue({
      mutate: vi.fn(), isPending: true, isSuccess: false, isError: false,
    } as any)
    vi.mocked(useUpdateTrainingPhase).mockReturnValue({
      mutate: vi.fn(), isPending: true, isSuccess: false, isError: false,
    } as any)

    renderSettings()
    const savingButtons = screen.getAllByRole('button', { name: /saving/i })
    savingButtons.forEach((btn) => expect(btn).toBeDisabled())
  })
})
