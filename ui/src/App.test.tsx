import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'

vi.mock('@azure/msal-browser', () => ({
  PublicClientApplication: vi.fn(function () {}),
}))

vi.mock('@azure/msal-react', () => ({
  useIsAuthenticated: vi.fn(),
}))

// Stub the pages so their own dependencies (useAuth, React Query, etc.) don't
// need to be wired up — routing logic is what we're testing here.
vi.mock('./pages/LoginPage', () => ({
  LoginPage: () => <div>Login Page</div>,
}))
vi.mock('./pages/HomePage', () => ({
  HomePage: () => <div>Home Page</div>,
}))
vi.mock('./pages/StravaConnectedPage', () => ({
  StravaConnectedPage: () => <div>Strava Connected Page</div>,
}))

import { useIsAuthenticated } from '@azure/msal-react'
import App from './App'

describe('ProtectedRoute', () => {
  it('renders children when the user is authenticated', () => {
    vi.mocked(useIsAuthenticated).mockReturnValue(true)
    window.history.pushState({}, '', '/home')

    render(<App />)

    expect(screen.getByText('Home Page')).toBeInTheDocument()
  })

  it('redirects to / and shows LoginPage when the user is not authenticated', () => {
    vi.mocked(useIsAuthenticated).mockReturnValue(false)
    window.history.pushState({}, '', '/home')

    render(<App />)

    expect(screen.getByText('Login Page')).toBeInTheDocument()
  })
})

describe('RootRoute', () => {
  it('shows LoginPage when the user is not authenticated', () => {
    vi.mocked(useIsAuthenticated).mockReturnValue(false)
    window.history.pushState({}, '', '/')

    render(<App />)

    expect(screen.getByText('Login Page')).toBeInTheDocument()
  })

  it('redirects to /home when the user is authenticated', () => {
    vi.mocked(useIsAuthenticated).mockReturnValue(true)
    window.history.pushState({}, '', '/')

    render(<App />)

    expect(screen.getByText('Home Page')).toBeInTheDocument()
  })
})
