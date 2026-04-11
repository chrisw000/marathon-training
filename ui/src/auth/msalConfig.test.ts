import { describe, it, expect, vi } from 'vitest'

// PublicClientApplication calls browser APIs at construction time;
// mock the module before importing the file under test.
vi.mock('@azure/msal-browser', () => ({
  // Must be a real class/function — arrow functions cannot be used with `new`
  PublicClientApplication: vi.fn(function () {}),
}))

const { apiScopes } = await import('./msalConfig')

describe('msalConfig', () => {
  it('exports apiScopes as a non-empty array', () => {
    expect(Array.isArray(apiScopes)).toBe(true)
    expect(apiScopes.length).toBeGreaterThan(0)
  })
})
