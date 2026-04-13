import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { NavBar } from './NavBar'

function renderNav(initialPath = '/dashboard') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <NavBar />
    </MemoryRouter>,
  )
}

describe('NavBar', () => {
  it('renders Dashboard, Activities and Settings links', () => {
    renderNav()
    expect(screen.getByRole('link', { name: 'Dashboard' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Activities' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Settings' })).toBeInTheDocument()
  })

  it('Dashboard link points to /dashboard', () => {
    renderNav()
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('href', '/dashboard')
  })

  it('Activities link points to /activities', () => {
    renderNav()
    expect(screen.getByRole('link', { name: 'Activities' })).toHaveAttribute('href', '/activities')
  })

  it('Settings link points to /settings', () => {
    renderNav()
    expect(screen.getByRole('link', { name: 'Settings' })).toHaveAttribute('href', '/settings')
  })
})
