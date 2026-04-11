import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { LoginPage } from './LoginPage'

vi.mock('../auth/useAuth', () => ({
  useAuth: () => ({ login: vi.fn() }),
}))

describe('LoginPage', () => {
  it('renders a sign-in button', () => {
    render(<LoginPage />)
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })
})
