import { useState } from 'react';
import { useAuth } from '../auth/useAuth';

export function HomePage() {
  const { user, logout, getAccessToken } = useAuth();
  const [copied, setCopied] = useState(false);

  const copyToken = async () => {
    const token = await getAccessToken();
    await navigator.clipboard.writeText(token);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <main>
      <h1>Welcome, {user?.name}</h1>
      <button onClick={() => void logout()}>Sign out</button>
      <button onClick={() => void copyToken()}>{copied ? 'Copied!' : 'Copy API token'}</button>
    </main>
  );
}
