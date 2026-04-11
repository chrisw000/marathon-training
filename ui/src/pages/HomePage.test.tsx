import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { HomePage } from './HomePage'

vi.mock('../auth/useAuth', () => ({
  useAuth: () => ({
    user: { name: 'Ada Lovelace', email: 'ada@example.com' },
    logout: vi.fn(),
  }),
}))

describe('HomePage', () => {
  it('renders the user display name', () => {
    render(<HomePage />)
    expect(screen.getByText(/Ada Lovelace/)).toBeInTheDocument()
  })

  it('renders a sign-out button', () => {
    render(<HomePage />)
    expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
  })
})
