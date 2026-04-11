import { type Configuration, PublicClientApplication } from '@azure/msal-browser';

const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID as string,
    authority: import.meta.env.VITE_ENTRA_AUTHORITY as string,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    // sessionStorage is scoped to the tab — safer for multi-account scenarios
    cacheLocation: 'sessionStorage',
  },
};

// Scopes the API access token will be requested for
export const apiScopes: string[] = [import.meta.env.VITE_API_SCOPE as string];

// Single shared instance — must not be recreated on re-render
export const msalInstance = new PublicClientApplication(msalConfig);
