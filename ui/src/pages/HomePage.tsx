import { useAuth } from '../auth/useAuth';

export function HomePage() {
  const { user, logout } = useAuth();

  return (
    <main>
      <h1>Welcome, {user?.name}</h1>
      <button onClick={() => void logout()}>Sign out</button>
    </main>
  );
}
