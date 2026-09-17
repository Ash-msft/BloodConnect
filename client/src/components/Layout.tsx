import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import type { TeamsHostInfo } from '../context/useTeamsContext'
import { DemoUserSwitcher } from './DemoUserSwitcher'

interface LayoutProps {
  teamsHostInfo: TeamsHostInfo
}

export function Layout({ teamsHostInfo }: LayoutProps) {
  const { currentUser, signOut } = useAuth()

  return (
    <div className={`app-shell theme-${teamsHostInfo.theme}`}>
      <header className="app-header">
        <div className="app-header__brand">
          <span aria-hidden="true">🩸</span>
          <span>BloodConnect</span>
        </div>
        {currentUser && (
          <nav className="app-nav" aria-label="Primary">
            <NavLink to="/" end>
              Dashboard
            </NavLink>
            <NavLink to="/profile">My Profile</NavLink>
            <NavLink to="/requests">My Requests</NavLink>
            <NavLink to="/matches">Matched Requests</NavLink>
            <NavLink to="/notifications">Notifications</NavLink>
          </nav>
        )}
        <div className="app-header__account">
          {currentUser && (
            <>
              <span className="app-header__username">{currentUser.displayName}</span>
              <button type="button" className="btn btn--ghost" onClick={signOut}>
                Switch user
              </button>
            </>
          )}
        </div>
      </header>

      {!teamsHostInfo.isHostedInTeams && (
        <div className="banner banner--info" role="note">
          Running in browser demo mode (not inside Microsoft Teams). Identity is selected locally —
          see the README for how Entra ID replaces this in production.
        </div>
      )}

      <main className="app-main">
        {currentUser ? <Outlet /> : <DemoUserSwitcher />}
      </main>

      <footer className="app-footer">
        <p>
          <strong>Medical disclaimer:</strong> Eligibility and matching information in BloodConnect is
          informational only. Always follow your donation center's clinical screening and eligibility
          rules — they take precedence over anything shown here.
        </p>
      </footer>
    </div>
  )
}
