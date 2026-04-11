import { useAuth } from '../auth/useAuth';

export function LoginPage() {
  const { login } = useAuth();

  return (
    <main>
      <h1>Marathon Trainer</h1>
      <p>Sign in to view your training plan.</p>
      <button onClick={() => void login()}>Sign in</button>
    </main>
  );
}
