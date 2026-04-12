import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { StravaConnectedPage } from './StravaConnectedPage'

describe('StravaConnectedPage', () => {
  it('renders the connected heading', () => {
    render(
      <MemoryRouter>
        <StravaConnectedPage />
      </MemoryRouter>,
    )
    expect(screen.getByRole('heading', { name: /strava connected/i })).toBeInTheDocument()
  })

  it('renders the success message', () => {
    render(
      <MemoryRouter>
        <StravaConnectedPage />
      </MemoryRouter>,
    )
    expect(screen.getByText(/linked successfully/i)).toBeInTheDocument()
  })

  it('renders a link back to home', () => {
    render(
      <MemoryRouter>
        <StravaConnectedPage />
      </MemoryRouter>,
    )
    expect(screen.getByRole('link', { name: /back to home/i })).toBeInTheDocument()
  })
})
