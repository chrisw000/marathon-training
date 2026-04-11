import { useEffect, useState } from 'react';
import { useAuth } from '../auth/useAuth';
import { useEnsureProfile, useStravaStatus, useStravaAuthorise } from '../api/marathonApi';

export function HomePage() {
  const { user, logout, getAccessToken } = useAuth();
  const [copied, setCopied] = useState(false);

  const ensureProfile = useEnsureProfile();
  const stravaStatus = useStravaStatus();
  const stravaAuthorise = useStravaAuthorise();

  // Ensure the athlete profile exists as soon as the home page mounts.
  // This is idempotent — safe to call on every login.
  useEffect(() => {
    ensureProfile.mutate(undefined, {
      onSuccess: () => {
        // Refetch Strava status now that we know the profile exists
        void stravaStatus.refetch();
      },
    });
  // ensureProfile.mutate is stable across renders; stravaStatus.refetch is too.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const copyToken = async () => {
    const token = await getAccessToken();
    await navigator.clipboard.writeText(token);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const connectStrava = () => {
    stravaAuthorise.mutate();
  };

  return (
    <main>
      <h1>Welcome, {user?.name}</h1>

      {stravaStatus.isLoading && <p>Checking Strava connection…</p>}

      {stravaStatus.data?.isConnected ? (
        <p>Strava connected ✓</p>
      ) : (
        stravaStatus.data && (
          <button
            onClick={connectStrava}
            disabled={stravaAuthorise.isPending}
          >
            {stravaAuthorise.isPending ? 'Redirecting…' : 'Connect Strava'}
          </button>
        )
      )}

      <button onClick={() => void logout()}>Sign out</button>
      <button onClick={() => void copyToken()}>{copied ? 'Copied!' : 'Copy API token'}</button>
    </main>
  );
}
