import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { useIsAuthenticated } from '@azure/msal-react';
import { LoginPage } from './pages/LoginPage';
import { HomePage } from './pages/HomePage';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useIsAuthenticated();
  return isAuthenticated ? <>{children}</> : <Navigate to="/" replace />;
}

function RootRoute() {
  const isAuthenticated = useIsAuthenticated();
  return isAuthenticated ? <Navigate to="/home" replace /> : <LoginPage />;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<RootRoute />} />
        <Route
          path="/home"
          element={
            <ProtectedRoute>
              <HomePage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
