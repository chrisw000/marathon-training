import { useMutation, useQuery } from '@tanstack/react-query';
import { useAuth } from '../auth/useAuth';

const API_BASE = import.meta.env.VITE_API_BASE_URL as string;

async function apiRequest<T>(
  path: string,
  token: string,
  options?: RequestInit,
): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}

// ── Profile ──────────────────────────────────────────────────────────────────

interface EnsureProfileResult {
  created: boolean;
}

export function useEnsureProfile() {
  const { getAccessToken } = useAuth();

  return useMutation({
    mutationFn: async (): Promise<EnsureProfileResult> => {
      const token = await getAccessToken();
      return apiRequest('/api/profile', token, { method: 'POST' });
    },
  });
}

// ── Strava ───────────────────────────────────────────────────────────────────

export interface StravaStatus {
  isConnected: boolean;
  stravaAthleteId: number | null;
  expiresAt: string | null;
}

export function useStravaStatus() {
  const { getAccessToken } = useAuth();

  return useQuery({
    queryKey: ['strava-status'],
    queryFn: async (): Promise<StravaStatus> => {
      const token = await getAccessToken();
      return apiRequest('/api/strava/status', token);
    },
  });
}

interface StravaAuthoriseResult {
  url: string;
}

export function useStravaAuthorise() {
  const { getAccessToken } = useAuth();

  return useMutation({
    mutationFn: async (): Promise<StravaAuthoriseResult> => {
      const token = await getAccessToken();
      return apiRequest('/api/strava/authorise', token);
    },
    onSuccess: ({ url }) => {
      window.location.href = url;
    },
  });
}
