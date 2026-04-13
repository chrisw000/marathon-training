import { useState, useEffect } from 'react';
import { useAthleteProfile, useUpdatePhysiology, useUpdateTrainingPhase } from '../api/marathonApi';

const TRAINING_PHASES = ['Base', 'Build', 'RaceDevelopment', 'Peak', 'Taper'] as const;

const PHASE_LABELS: Record<string, string> = {
  Base: 'Base — building aerobic foundation',
  Build: 'Build — increasing intensity and volume',
  RaceDevelopment: 'Race development — race-specific work',
  Peak: 'Peak — sharpening before target race',
  Taper: 'Taper — reducing load before race day',
};

export function SettingsPage() {
  const profile = useAthleteProfile();
  const updatePhysiology = useUpdatePhysiology();
  const updatePhase = useUpdateTrainingPhase();

  // Physiology form state
  const [restingHr, setRestingHr] = useState('');
  const [maxHr, setMaxHr] = useState('');
  const [thresholdHr, setThresholdHr] = useState('');
  const [ftpWatts, setFtpWatts] = useState('');

  // Phase form state
  const [selectedPhase, setSelectedPhase] = useState('');

  // Pre-populate form once profile loads
  useEffect(() => {
    if (!profile.data) return;
    if (profile.data.restingHr) setRestingHr(String(profile.data.restingHr));
    if (profile.data.maxHr) setMaxHr(String(profile.data.maxHr));
    if (profile.data.thresholdHr) setThresholdHr(String(profile.data.thresholdHr));
    if (profile.data.ftpWatts) setFtpWatts(String(profile.data.ftpWatts));
    if (profile.data.currentPhase) setSelectedPhase(profile.data.currentPhase);
  }, [profile.data]);

  const handleSavePhysiology = (e: React.FormEvent) => {
    e.preventDefault();
    updatePhysiology.mutate({
      restingHr: Number(restingHr),
      maxHr: Number(maxHr),
      thresholdHr: Number(thresholdHr),
      ftpWatts: Number(ftpWatts),
    });
  };

  const handleSavePhase = (e: React.FormEvent) => {
    e.preventDefault();
    updatePhase.mutate(selectedPhase);
  };

  if (profile.isLoading) return <main><p>Loading settings…</p></main>;

  if (profile.isError) return (
    <main>
      <h1>Settings</h1>
      <p>Unable to load profile. Please try again.</p>
    </main>
  );

  return (
    <main>
      <h1>Settings</h1>
      <p><strong>{profile.data?.displayName}</strong></p>

      {/* ── Training phase ──────────────────────────────────────────────── */}
      <section>
        <h2>Training phase</h2>
        <p>Your current phase determines the weekly TSS target range shown on the dashboard.</p>

        <form onSubmit={handleSavePhase}>
          <label htmlFor="phase">Phase</label>
          <select
            id="phase"
            value={selectedPhase}
            onChange={(e) => setSelectedPhase(e.target.value)}
          >
            {TRAINING_PHASES.map((p) => (
              <option key={p} value={p}>{PHASE_LABELS[p]}</option>
            ))}
          </select>

          <button type="submit" disabled={updatePhase.isPending || !selectedPhase}>
            {updatePhase.isPending ? 'Saving…' : 'Save phase'}
          </button>

          {updatePhase.isSuccess && <p>Phase saved ✓</p>}
          {updatePhase.isError && <p>Failed to save phase — please try again</p>}
        </form>
      </section>

      {/* ── Heart rate & power ───────────────────────────────────────────── */}
      <section>
        <h2>Heart rate &amp; power</h2>
        <p>
          Used to calculate hrTSS for runs and rides. FTP is used for power-based TSS on rides;
          enter a realistic estimate if you don&apos;t have a power meter (e.g. 200W).
        </p>

        <form onSubmit={handleSavePhysiology}>
          <label htmlFor="restingHr">Resting HR (bpm)</label>
          <input
            id="restingHr"
            type="number"
            min={1}
            max={100}
            value={restingHr}
            onChange={(e) => setRestingHr(e.target.value)}
            required
          />

          <label htmlFor="maxHr">Max HR (bpm)</label>
          <input
            id="maxHr"
            type="number"
            min={1}
            max={220}
            value={maxHr}
            onChange={(e) => setMaxHr(e.target.value)}
            required
          />

          <label htmlFor="thresholdHr">Threshold HR (bpm, must be &lt; Max HR)</label>
          <input
            id="thresholdHr"
            type="number"
            min={1}
            max={219}
            value={thresholdHr}
            onChange={(e) => setThresholdHr(e.target.value)}
            required
          />

          <label htmlFor="ftpWatts">FTP (watts, 1–599)</label>
          <input
            id="ftpWatts"
            type="number"
            min={1}
            max={599}
            value={ftpWatts}
            onChange={(e) => setFtpWatts(e.target.value)}
            required
          />

          <button type="submit" disabled={updatePhysiology.isPending}>
            {updatePhysiology.isPending ? 'Saving…' : 'Save physiology'}
          </button>

          {updatePhysiology.isSuccess && <p>Physiology saved ✓</p>}
          {updatePhysiology.isError && <p>Failed to save — check all values are valid</p>}
        </form>
      </section>
    </main>
  );
}
