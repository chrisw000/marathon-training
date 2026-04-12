import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'

vi.mock('@azure/msal-browser', () => ({
  PublicClientApplication: vi.fn(function () {}),
}))

vi.mock('@azure/msal-react', () => ({
  MsalProvider: vi.fn(({ children }: { children: React.ReactNode }) => <>{children}</>),
}))

import { MsalProvider } from '@azure/msal-react'
import { msalInstance } from './msalConfig'
import { AuthProvider } from './AuthProvider'

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders its children', () => {
    render(
      <AuthProvider>
        <p>test child</p>
      </AuthProvider>,
    )
    expect(screen.getByText('test child')).toBeInTheDocument()
  })

  it('passes msalInstance to MsalProvider', () => {
    render(
      <AuthProvider>
        <p>child</p>
      </AuthProvider>,
    )
    const [props] = vi.mocked(MsalProvider).mock.calls[0]
    expect(props.instance).toBe(msalInstance)
  })
})
