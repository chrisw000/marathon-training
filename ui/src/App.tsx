import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { useIsAuthenticated } from '@azure/msal-react';
import { Layout } from './components/Layout';
import { LoginPage } from './pages/LoginPage';
import { HomePage } from './pages/HomePage';
import { StravaConnectedPage } from './pages/StravaConnectedPage';
import { DashboardPage } from './pages/DashboardPage';
import { SettingsPage } from './pages/SettingsPage';
import { ActivitiesPage } from './pages/ActivitiesPage';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useIsAuthenticated();
  return isAuthenticated ? <Layout>{children}</Layout> : <Navigate to="/" replace />;
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
        <Route
          path="/strava-connected"
          element={
            <ProtectedRoute>
              <StravaConnectedPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/activities"
          element={
            <ProtectedRoute>
              <ActivitiesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/settings"
          element={
            <ProtectedRoute>
              <SettingsPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
