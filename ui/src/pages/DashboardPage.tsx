import { useTrainingLoad, useWeekSummary } from '../api/marathonApi';

/** Returns ISO date string for the Monday of the current week. */
function currentWeekStart(): string {
  const today = new Date();
  const dow = today.getDay(); // 0 = Sunday
  const daysToMonday = dow === 0 ? 6 : dow - 1;
  const monday = new Date(today);
  monday.setDate(today.getDate() - daysToMonday);
  return monday.toISOString().slice(0, 10);
}

/** Returns ISO date string for N days ago. */
function daysAgo(n: number): string {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d.toISOString().slice(0, 10);
}

function tssBar(tss: number, total: number) {
  if (total === 0) return '0%';
  return `${Math.round((tss / total) * 100)}%`;
}

export function DashboardPage() {
  const weekStart = currentWeekStart();
  const loadFrom = daysAgo(27); // 4 weeks back
  const loadTo = daysAgo(0);

  const weekSummary = useWeekSummary(weekStart);
  const trainingLoad = useTrainingLoad(loadFrom, loadTo);

  return (
    <main>
      <h1>Training dashboard</h1>

      {/* ── This week ────────────────────────────────────────────────────── */}
      <section>
        <h2>This week</h2>

        {weekSummary.isLoading && <p>Loading week summary…</p>}

        {weekSummary.isError && (
          <p>No training data yet for this week.</p>
        )}

        {weekSummary.data && (
          <>
            <table>
              <tbody>
                <tr>
                  <th scope="row">Total TSS</th>
                  <td>{weekSummary.data.totalTss.toFixed(0)}</td>
                </tr>
                <tr>
                  <th scope="row">Runs</th>
                  <td>
                    {weekSummary.data.runCount} ({weekSummary.data.runTss.toFixed(0)} TSS,{' '}
                    {tssBar(weekSummary.data.runTss, weekSummary.data.totalTss)})
                  </td>
                </tr>
                <tr>
                  <th scope="row">Rides</th>
                  <td>
                    {weekSummary.data.rideCount} ({weekSummary.data.rideTss.toFixed(0)} TSS,{' '}
                    {tssBar(weekSummary.data.rideTss, weekSummary.data.totalTss)})
                  </td>
                </tr>
                <tr>
                  <th scope="row">Strength</th>
                  <td>
                    {weekSummary.data.strengthCount} ({weekSummary.data.strengthTss.toFixed(0)} TSS,{' '}
                    {tssBar(weekSummary.data.strengthTss, weekSummary.data.totalTss)})
                  </td>
                </tr>
                <tr>
                  <th scope="row">Form (TSB)</th>
                  <td>{weekSummary.data.trainingLoad.tsb.toFixed(1)} — {weekSummary.data.trainingLoad.formDescription}</td>
                </tr>
                <tr>
                  <th scope="row">Fitness (CTL)</th>
                  <td>{weekSummary.data.trainingLoad.ctl.toFixed(1)}</td>
                </tr>
                <tr>
                  <th scope="row">Fatigue (ATL)</th>
                  <td>{weekSummary.data.trainingLoad.atl.toFixed(1)}</td>
                </tr>
              </tbody>
            </table>

            {weekSummary.data.hasOvertrainingWarning && (
              <p role="alert">⚠ Overtraining risk — consider an easy day</p>
            )}
            {weekSummary.data.trainingLoad.isRaceReady && (
              <p>Race ready ✓</p>
            )}

            <p><strong>Recommendation:</strong> {weekSummary.data.recommendation}</p>
          </>
        )}
      </section>

      {/* ── Last 4 weeks ──────────────────────────────────────────────────── */}
      <section>
        <h2>Last 4 weeks — training load</h2>

        {trainingLoad.isLoading && <p>Loading training load…</p>}

        {trainingLoad.data && trainingLoad.data.length === 0 && (
          <p>No training data in this period.</p>
        )}

        {trainingLoad.data && trainingLoad.data.length > 0 && (
          <table>
            <thead>
              <tr>
                <th scope="col">Date</th>
                <th scope="col">TSS</th>
                <th scope="col">ATL</th>
                <th scope="col">CTL</th>
                <th scope="col">TSB</th>
                <th scope="col">Form</th>
              </tr>
            </thead>
            <tbody>
              {trainingLoad.data
                .filter((d) => d.dailyTss > 0 || d.ctl > 0)
                .map((d) => (
                  <tr key={d.date} style={d.isOvertrainingDanger ? { color: 'red' } : d.isOvertrainingWarning ? { color: 'orange' } : undefined}>
                    <td>{d.date}</td>
                    <td>{d.dailyTss.toFixed(0)}</td>
                    <td>{d.atl.toFixed(1)}</td>
                    <td>{d.ctl.toFixed(1)}</td>
                    <td>{d.tsb.toFixed(1)}</td>
                    <td>{d.formDescription}</td>
                  </tr>
                ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}
