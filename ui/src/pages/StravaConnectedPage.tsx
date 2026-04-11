import { Link } from 'react-router-dom';

export function StravaConnectedPage() {
  return (
    <main>
      <h1>Strava Connected</h1>
      <p>Your Strava account has been linked successfully.</p>
      <Link to="/home">Back to home</Link>
    </main>
  );
}
