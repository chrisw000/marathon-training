import { NavLink } from 'react-router-dom';

export function NavBar() {
  return (
    <nav aria-label="Main navigation">
      <NavLink to="/dashboard">Dashboard</NavLink>
      {' | '}
      <NavLink to="/activities">Activities</NavLink>
      {' | '}
      <NavLink to="/settings">Settings</NavLink>
    </nav>
  );
}
