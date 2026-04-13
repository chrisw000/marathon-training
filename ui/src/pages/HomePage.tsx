import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';
import {
  useEnsureProfile,
  useStravaStatus,
  useStravaAuthorise,
  useSyncActivities,
  useActivities,
} from '../api/marathonApi';

export function HomePage() {
  const { user, logout } = useAuth();

  const ensureProfile = useEnsureProfile();
  const stravaStatus = useStravaStatus();
  const stravaAuthorise = useStravaAuthorise();
  const syncActivities = useSyncActivities();
  const activities = useActivities({ pageSize: 10 });

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

  const handleSync = () => {
    syncActivities.mutate(undefined, {
      onSuccess: () => {
        void activities.refetch();
      },
    });
  };

  return (
    <main>
      <h1>Welcome, {user?.name}</h1>

      {stravaStatus.isLoading && <p>Checking Strava connection…</p>}

      {stravaStatus.data?.isConnected ? (
        <section>
          <p>Strava connected ✓</p>
          <Link to="/dashboard">View training dashboard</Link>
          <button
            onClick={handleSync}
            disabled={syncActivities.isPending}
          >
            {syncActivities.isPending ? 'Syncing…' : 'Sync activities'}
          </button>
          {syncActivities.isSuccess && (
            <p>{syncActivities.data.activitiesSynced} activities synced</p>
          )}
          {syncActivities.isError && (
            <p>Sync failed — please try again</p>
          )}
        </section>
      ) : (
        stravaStatus.data && (
          <button
            onClick={() => stravaAuthorise.mutate()}
            disabled={stravaAuthorise.isPending}
          >
            {stravaAuthorise.isPending ? 'Redirecting…' : 'Connect Strava'}
          </button>
        )
      )}

      {activities.data && activities.data.items.length > 0 && (
        <section>
          <h2>Recent activities</h2>
          <ul>
            {activities.data.items.map((a) => (
              <li key={a.id}>
                {a.name} — {a.activityType}
                {a.tssScore != null && ` — TSS ${a.tssScore}`}
              </li>
            ))}
          </ul>
        </section>
      )}

      <button onClick={() => void logout()}>Sign out</button>
    </main>
  );
}
