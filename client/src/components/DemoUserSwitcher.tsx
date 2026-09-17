import { useAuth } from '../context/useAuth'

/**
 * Local demo identity selector. Only used outside Teams / before a real Entra ID login is wired up
 * (see README "Local demo auth limitations"). The browser can only pick among a fixed, server-known
 * set of demo identities — it never submits arbitrary identity data as authoritative.
 */
export function DemoUserSwitcher() {
  const { demoUsers, selectDemoUser, error, isLoading } = useAuth()

  return (
    <div className="card demo-switcher">
      <h1>Welcome to BloodConnect</h1>
      <p>
        Select a demo employee identity to continue. This local demo auth mechanism exists only for
        development/testing — a real deployment replaces it with Entra ID sign-in (see README).
      </p>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {isLoading && demoUsers.length === 0 ? (
        <p>Loading demo identities…</p>
      ) : (
        <ul className="demo-switcher__list">
          {demoUsers.map((user) => (
            <li key={user.externalId}>
              <button
                type="button"
                className="btn btn--secondary demo-switcher__option"
                onClick={() => selectDemoUser(user.externalId)}
              >
                {user.displayName}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
