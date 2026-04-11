import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { apiScopes } from './msalConfig';

interface AuthUser {
  name: string;
  email: string;
}

interface UseAuthResult {
  isAuthenticated: boolean;
  user: AuthUser | null;
  login: () => Promise<void>;
  logout: () => Promise<void>;
  getAccessToken: () => Promise<string>;
}

export function useAuth(): UseAuthResult {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const account = accounts[0] ?? null;

  const user: AuthUser | null = account
    ? {
        // account.name is the display name from the id token
        name: account.name ?? '',
        // account.username is the UPN / email for Entra accounts
        email: account.username ?? '',
      }
    : null;

  const login = () =>
    instance.loginRedirect({ scopes: apiScopes });

  const logout = () =>
    instance.logoutRedirect({ account: account ?? undefined });

  const getAccessToken = async (): Promise<string> => {
    if (!account) throw new Error('No account is signed in.');

    try {
      const result = await instance.acquireTokenSilent({
        scopes: apiScopes,
        account,
      });
      return result.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        // Silent renewal failed (e.g. expired refresh token) — fall back to redirect
        await instance.acquireTokenRedirect({ scopes: apiScopes, account });
      }
      throw error;
    }
  };

  return { isAuthenticated, user, login, logout, getAccessToken };
}
