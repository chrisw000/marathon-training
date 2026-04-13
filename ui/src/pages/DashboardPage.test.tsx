import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { DashboardPage } from './DashboardPage'

vi.mock('../auth/useAuth', () => ({
  useAuth: () => ({
    getAccessToken: vi.fn().mockResolvedValue('test-token'),
  }),
}))

vi.mock('../api/marathonApi', () => ({
  useWeekSummary: vi.fn(),
  useTrainingLoad: vi.fn(),
}))

import { useWeekSummary, useTrainingLoad } from '../api/marathonApi'

function setupDefaultMocks() {
  vi.mocked(useWeekSummary).mockReturnValue({
    isLoading: false,
    isError: false,
    data: undefined,
  } as any)

  vi.mocked(useTrainingLoad).mockReturnValue({
    isLoading: false,
    data: undefined,
  } as any)
}

function renderDashboard() {
  return render(
    <MemoryRouter>
      <DashboardPage />
    </MemoryRouter>,
  )
}

describe('DashboardPage', () => {
  it('renders the page heading', () => {
    setupDefaultMocks()
    renderDashboard()
    expect(screen.getByRole('heading', { name: /training dashboard/i })).toBeInTheDocument()
  })

  it('shows loading state while week summary is loading', () => {
    vi.mocked(useWeekSummary).mockReturnValue({ isLoading: true, isError: false, data: undefined } as any)
    vi.mocked(useTrainingLoad).mockReturnValue({ isLoading: false, data: undefined } as any)

    renderDashboard()
    expect(screen.getByText(/loading week summary/i)).toBeInTheDocument()
  })

  it('shows "no data yet" when week summary returns 404', () => {
    vi.mocked(useWeekSummary).mockReturnValue({ isLoading: false, isError: true, data: undefined } as any)
    vi.mocked(useTrainingLoad).mockReturnValue({ isLoading: false, data: undefined } as any)

    renderDashboard()
    expect(screen.getByText(/no training data yet/i)).toBeInTheDocument()
  })

  it('renders week summary data when available', () => {
    vi.mocked(useWeekSummary).mockReturnValue({
      isLoading: false,
      isError: false,
      data: {
        weekStart: '2026-04-07',
        totalTss: 320,
        runCount: 3,
        rideCount: 1,
        strengthCount: 2,
        runTss: 210,
        rideTss: 80,
        strengthTss: 30,
        trainingLoad: { tsb: -5.2, ctl: 45.1, atl: 50.3, formDescription: 'Productive', isRaceReady: false, isOvertrainingWarning: false, isOvertrainingDanger: false, date: '2026-04-13', dailyTss: 0 },
        hasOvertrainingWarning: false,
        recommendation: 'On track',
      },
    } as any)
    vi.mocked(useTrainingLoad).mockReturnValue({ isLoading: false, data: undefined } as any)

    renderDashboard()
    expect(screen.getByText('320')).toBeInTheDocument()
    expect(screen.getByText(/on track/i)).toBeInTheDocument()
    expect(screen.getByText(/productive/i)).toBeInTheDocument()
  })

  it('shows overtraining warning when flagged', () => {
    vi.mocked(useWeekSummary).mockReturnValue({
      isLoading: false,
      isError: false,
      data: {
        weekStart: '2026-04-07',
        totalTss: 700,
        runCount: 5, rideCount: 0, strengthCount: 0,
        runTss: 700, rideTss: 0, strengthTss: 0,
        trainingLoad: { tsb: -35, ctl: 60, atl: 95, formDescription: 'Very tired', isRaceReady: false, isOvertrainingWarning: true, isOvertrainingDanger: false, date: '2026-04-13', dailyTss: 0 },
        hasOvertrainingWarning: true,
        recommendation: 'Reduce load this week',
      },
    } as any)
    vi.mocked(useTrainingLoad).mockReturnValue({ isLoading: false, data: undefined } as any)

    renderDashboard()
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText(/overtraining risk/i)).toBeInTheDocument()
  })

  it('shows "race ready" when flag is set', () => {
    vi.mocked(useWeekSummary).mockReturnValue({
      isLoading: false,
      isError: false,
      data: {
        weekStart: '2026-04-07',
        totalTss: 200,
        runCount: 3, rideCount: 0, strengthCount: 0,
        runTss: 200, rideTss: 0, strengthTss: 0,
        trainingLoad: { tsb: 15, ctl: 55, atl: 40, formDescription: 'Race ready', isRaceReady: true, isOvertrainingWarning: false, isOvertrainingDanger: false, date: '2026-04-13', dailyTss: 0 },
        hasOvertrainingWarning: false,
        recommendation: 'On track',
      },
    } as any)
    vi.mocked(useTrainingLoad).mockReturnValue({ isLoading: false, data: undefined } as any)

    renderDashboard()
    expect(screen.getByText('Race ready ✓')).toBeInTheDocument()
  })

  it('renders training load table when data is available', () => {
    setupDefaultMocks()
    vi.mocked(useTrainingLoad).mockReturnValue({
      isLoading: false,
      data: [
        { date: '2026-04-10', dailyTss: 85, atl: 52.1, ctl: 47.3, tsb: -4.8, formDescription: 'Productive', isOvertrainingWarning: false, isOvertrainingDanger: false, isRaceReady: false },
        { date: '2026-04-11', dailyTss: 0, atl: 48.5, ctl: 47.5, tsb: -1.0, formDescription: 'Fresh', isOvertrainingWarning: false, isOvertrainingDanger: false, isRaceReady: false },
      ],
    } as any)

    renderDashboard()
    expect(screen.getByText('2026-04-10')).toBeInTheDocument()
    expect(screen.getByText('85')).toBeInTheDocument()
    expect(screen.getByText('Productive')).toBeInTheDocument()
  })

  it('shows empty state message when no training load data', () => {
    setupDefaultMocks()
    vi.mocked(useTrainingLoad).mockReturnValue({ isLoading: false, data: [] } as any)

    renderDashboard()
    expect(screen.getByText(/no training data in this period/i)).toBeInTheDocument()
  })
})
