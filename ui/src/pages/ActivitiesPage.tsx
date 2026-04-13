import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useActivities, useSyncActivities, useLogManualActivity, useStravaStatus } from '../api/marathonApi';

const ACTIVITY_TYPES = ['All', 'Run', 'Ride', 'Strength'] as const;
type ActivityTypeFilter = (typeof ACTIVITY_TYPES)[number];

function formatDuration(seconds: number): string {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  if (h > 0) return `${h}h ${m}m`;
  return `${m}m`;
}

function formatDistance(metres: number | null): string {
  if (metres == null) return '—';
  return `${(metres / 1000).toFixed(1)} km`;
}

export function ActivitiesPage() {
  const [typeFilter, setTypeFilter] = useState<ActivityTypeFilter>('All');
  const [page, setPage] = useState(1);

  const activities = useActivities({
    type: typeFilter === 'All' ? undefined : typeFilter,
    page,
    pageSize: 20,
  });

  const stravaStatus = useStravaStatus();
  const sync = useSyncActivities();
  const logActivity = useLogManualActivity();

  // Manual log form state
  const [logName, setLogName] = useState('');
  const [logDate, setLogDate] = useState('');
  const [logDuration, setLogDuration] = useState('');
  const [logRpe, setLogRpe] = useState('');
  const [logNotes, setLogNotes] = useState('');

  const handleSync = () => {
    sync.mutate();
  };

  const handleLogSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    logActivity.mutate(
      {
        name: logName,
        activityType: 'Strength',
        startedAt: new Date(logDate).toISOString(),
        durationMinutes: Number(logDuration),
        rpe: Number(logRpe),
        notes: logNotes || undefined,
      },
      {
        onSuccess: () => {
          setLogName('');
          setLogDate('');
          setLogDuration('');
          setLogRpe('');
          setLogNotes('');
          activities.refetch();
        },
      },
    );
  };

  return (
    <main>
      <h1>Activities</h1>

      {/* ── Strava sync ────────────────────────────────────────────────────── */}
      <section>
        <h2>Strava sync</h2>

        {stravaStatus.data?.isConnected === false && (
          <p>
            Strava is not connected. <Link to="/home">Connect Strava</Link> to enable sync.
          </p>
        )}

        {stravaStatus.data?.isConnected && (
          <>
            <button onClick={handleSync} disabled={sync.isPending}>
              {sync.isPending ? 'Syncing…' : 'Sync from Strava'}
            </button>
            {sync.isSuccess && (
              <p>
                Synced {sync.data.activitiesSynced} activities
                {sync.data.activitiesSkipped > 0 && `, ${sync.data.activitiesSkipped} skipped`}.
              </p>
            )}
            {sync.isError && <p role="alert">Sync failed — please try again.</p>}
          </>
        )}
      </section>

      {/* ── Activity list ───────────────────────────────────────────────────── */}
      <section>
        <h2>Activity list</h2>

        <label htmlFor="typeFilter">Type</label>
        <select
          id="typeFilter"
          value={typeFilter}
          onChange={(e) => {
            setTypeFilter(e.target.value as ActivityTypeFilter);
            setPage(1);
          }}
        >
          {ACTIVITY_TYPES.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>

        {activities.isLoading && <p>Loading activities…</p>}
        {activities.isError && <p role="alert">Failed to load activities.</p>}

        {activities.data && activities.data.items.length === 0 && (
          <p>No activities found. Sync from Strava or log a strength session below.</p>
        )}

        {activities.data && activities.data.items.length > 0 && (
          <>
            <table>
              <thead>
                <tr>
                  <th scope="col">Date</th>
                  <th scope="col">Name</th>
                  <th scope="col">Type</th>
                  <th scope="col">Duration</th>
                  <th scope="col">Distance</th>
                  <th scope="col">TSS</th>
                </tr>
              </thead>
              <tbody>
                {activities.data.items.map((a) => (
                  <tr key={a.id}>
                    <td>{a.startedAt.slice(0, 10)}</td>
                    <td>{a.name}</td>
                    <td>{a.activityType}</td>
                    <td>{formatDuration(a.durationSeconds)}</td>
                    <td>{formatDistance(a.distanceMetres)}</td>
                    <td>{a.tssScore != null ? a.tssScore.toFixed(0) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <p>
              Page {activities.data.page} of {activities.data.totalPages} ({activities.data.totalCount} total)
            </p>
            <button
              onClick={() => setPage((p) => p - 1)}
              disabled={!activities.data.hasPreviousPage}
            >
              Previous
            </button>
            {' '}
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={!activities.data.hasNextPage}
            >
              Next
            </button>
          </>
        )}
      </section>

      {/* ── Log strength session ─────────────────────────────────────────────── */}
      <section>
        <h2>Log strength session</h2>
        <p>Manually record a strength or gym session. TSS is calculated from duration and RPE.</p>

        <form onSubmit={handleLogSubmit}>
          <label htmlFor="logName">Session name</label>
          <input
            id="logName"
            type="text"
            value={logName}
            onChange={(e) => setLogName(e.target.value)}
            placeholder="e.g. Gym — legs"
            required
          />

          <label htmlFor="logDate">Date and time</label>
          <input
            id="logDate"
            type="datetime-local"
            value={logDate}
            onChange={(e) => setLogDate(e.target.value)}
            required
          />

          <label htmlFor="logDuration">Duration (minutes, 1–479)</label>
          <input
            id="logDuration"
            type="number"
            min={1}
            max={479}
            value={logDuration}
            onChange={(e) => setLogDuration(e.target.value)}
            required
          />

          <label htmlFor="logRpe">RPE (1–10)</label>
          <input
            id="logRpe"
            type="number"
            min={1}
            max={10}
            value={logRpe}
            onChange={(e) => setLogRpe(e.target.value)}
            required
          />

          <label htmlFor="logNotes">Notes (optional)</label>
          <textarea
            id="logNotes"
            value={logNotes}
            onChange={(e) => setLogNotes(e.target.value)}
            rows={3}
          />

          <button type="submit" disabled={logActivity.isPending}>
            {logActivity.isPending ? 'Saving…' : 'Log session'}
          </button>

          {logActivity.isSuccess && (
            <p>
              Session logged
              {logActivity.data.tssScore != null
                ? ` — TSS: ${logActivity.data.tssScore.toFixed(0)}`
                : ''}
              .
            </p>
          )}
          {logActivity.isError && <p role="alert">Failed to log session — check all values are valid.</p>}
        </form>
      </section>
    </main>
  );
}
