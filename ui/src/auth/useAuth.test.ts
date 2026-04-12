import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'

// Mock @azure/msal-browser before any module imports it.
// PublicClientApplication is called at module level in msalConfig.ts;
// InteractionRequiredAuthError is used in the hook's catch branch.
vi.mock('@azure/msal-browser', () => {
  class InteractionRequiredAuthError extends Error {
    constructor(errorCode = 'interaction_required') {
      super(errorCode)
      this.name = 'InteractionRequiredAuthError'
    }
  }
  return {
    PublicClientApplication: vi.fn(function () {}),
    InteractionRequiredAuthError,
  }
})

const mockAcquireTokenSilent = vi.fn()
const mockAcquireTokenRedirect = vi.fn()
const mockLoginRedirect = vi.fn()
const mockLogoutRedirect = vi.fn()

vi.mock('@azure/msal-react', () => ({
  useMsal: vi.fn(),
  useIsAuthenticated: vi.fn(),
}))

import { useMsal, useIsAuthenticated } from '@azure/msal-react'
import { InteractionRequiredAuthError } from '@azure/msal-browser'
import { useAuth } from './useAuth'

function mockMsalWithAccount(account: object | null = null) {
  vi.mocked(useMsal).mockReturnValue({
    instance: {
      acquireTokenSilent: mockAcquireTokenSilent,
      acquireTokenRedirect: mockAcquireTokenRedirect,
      loginRedirect: mockLoginRedirect,
      logoutRedirect: mockLogoutRedirect,
    },
    accounts: account ? [account] : [],
    inProgress: 'none',
  } as any)
  vi.mocked(useIsAuthenticated).mockReturnValue(account !== null)
}

describe('useAuth', () => {
  beforeEach(() => {
    mockMsalWithAccount(null)
    mockAcquireTokenSilent.mockReset()
    mockAcquireTokenRedirect.mockReset()
  })

  it('returns isAuthenticated false when no account is present', () => {
    const { result } = renderHook(() => useAuth())
    expect(result.current.isAuthenticated).toBe(false)
  })

  it('returns user null when no account is present', () => {
    const { result } = renderHook(() => useAuth())
    expect(result.current.user).toBeNull()
  })

  it('returns isAuthenticated true and user data when an account exists', () => {
    mockMsalWithAccount({ name: 'Ada Lovelace', username: 'ada@example.com' })

    const { result } = renderHook(() => useAuth())

    expect(result.current.isAuthenticated).toBe(true)
    expect(result.current.user?.name).toBe('Ada Lovelace')
    expect(result.current.user?.email).toBe('ada@example.com')
  })

  it('getAccessToken throws when no account is signed in', async () => {
    const { result } = renderHook(() => useAuth())
    await expect(result.current.getAccessToken()).rejects.toThrow('No account is signed in.')
  })

  it('getAccessToken returns the token from acquireTokenSilent', async () => {
    mockMsalWithAccount({ name: 'Ada Lovelace', username: 'ada@example.com' })
    mockAcquireTokenSilent.mockResolvedValue({ accessToken: 'silent-token-xyz' })

    const { result } = renderHook(() => useAuth())
    const token = await result.current.getAccessToken()

    expect(token).toBe('silent-token-xyz')
    expect(mockAcquireTokenSilent).toHaveBeenCalledOnce()
  })

  it('getAccessToken falls back to acquireTokenRedirect on InteractionRequiredAuthError', async () => {
    mockMsalWithAccount({ name: 'Ada Lovelace', username: 'ada@example.com' })
    mockAcquireTokenSilent.mockRejectedValue(
      new InteractionRequiredAuthError('interaction_required'),
    )
    mockAcquireTokenRedirect.mockResolvedValue(undefined)

    const { result } = renderHook(() => useAuth())
    await expect(result.current.getAccessToken()).rejects.toBeInstanceOf(InteractionRequiredAuthError)
    expect(mockAcquireTokenRedirect).toHaveBeenCalledOnce()
  })
})
