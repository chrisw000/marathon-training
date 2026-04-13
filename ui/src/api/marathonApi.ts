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

// ── Activities ────────────────────────────────────────────────────────────────

export interface SyncResult {
  activitiesSynced: number;
  activitiesSkipped: number;
  syncedAt: string;
}

export function useSyncActivities() {
  const { getAccessToken } = useAuth();

  return useMutation({
    mutationFn: async (): Promise<SyncResult> => {
      const token = await getAccessToken();
      return apiRequest('/api/activities/sync', token, { method: 'POST' });
    },
  });
}

export interface ActivitySummary {
  id: string;
  name: string;
  activityType: string;
  startedAt: string;
  durationSeconds: number;
  distanceMetres: number | null;
  tssScore: number | null;
  averageHeartRateBpm: number | null;
}

export interface ActivityListResult {
  items: ActivitySummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export function useActivities(params?: { type?: string; page?: number; pageSize?: number }) {
  const { getAccessToken } = useAuth();

  const searchParams = new URLSearchParams();
  if (params?.type) searchParams.set('type', params.type);
  if (params?.page) searchParams.set('page', String(params.page));
  if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize));
  const qs = searchParams.size > 0 ? `?${searchParams.toString()}` : '';

  return useQuery({
    queryKey: ['activities', params],
    queryFn: async (): Promise<ActivityListResult> => {
      const token = await getAccessToken();
      return apiRequest(`/api/activities${qs}`, token);
    },
  });
}

// ── Training load ─────────────────────────────────────────────────────────────

export interface TrainingLoadDay {
  date: string;
  atl: number;
  ctl: number;
  tsb: number;
  dailyTss: number;
  isOvertrainingWarning: boolean;
  isOvertrainingDanger: boolean;
  isRaceReady: boolean;
  formDescription: string;
}

export function useTrainingLoad(from: string, to: string) {
  const { getAccessToken } = useAuth();

  return useQuery({
    queryKey: ['training-load', from, to],
    queryFn: async (): Promise<TrainingLoadDay[]> => {
      const token = await getAccessToken();
      return apiRequest(`/api/training/load?from=${from}&to=${to}`, token);
    },
    enabled: !!from && !!to,
  });
}

export interface WeekSummary {
  weekStart: string;
  totalTss: number;
  runCount: number;
  rideCount: number;
  strengthCount: number;
  runTss: number;
  rideTss: number;
  strengthTss: number;
  trainingLoad: TrainingLoadDay;
  hasOvertrainingWarning: boolean;
  recommendation: string;
}

export function useWeekSummary(weekStart: string) {
  const { getAccessToken } = useAuth();

  return useQuery({
    queryKey: ['week-summary', weekStart],
    queryFn: async (): Promise<WeekSummary> => {
      const token = await getAccessToken();
      return apiRequest(`/api/training/week/${weekStart}`, token);
    },
    enabled: !!weekStart,
    retry: (failureCount, error) => {
      // Don't retry 404 — week simply hasn't started yet
      if (error instanceof Error && error.message.includes('404')) return false;
      return failureCount < 2;
    },
  });
}
