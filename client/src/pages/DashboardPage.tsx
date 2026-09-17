import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../context/useAuth'
import type { BloodRequest, MatchedRequest, NotificationItem } from '../api/types'
import { ApiError } from '../api/client'
import { ErrorState, LoadingState } from '../components/StatusStates'
import { formatBloodGroup } from '../api/types'

export function DashboardPage() {
  const { apiClient, currentUser } = useAuth()
  const [myRequests, setMyRequests] = useState<BloodRequest[] | null>(null)
  const [matches, setMatches] = useState<MatchedRequest[] | null>(null)
  const [notifications, setNotifications] = useState<NotificationItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    // Deferred via microtask: state updates below happen inside async callbacks rather than
    // synchronously within the effect body itself.
    Promise.resolve().then(() => {
      setError(null)

      Promise.all([
        apiClient.get<BloodRequest[]>('/api/requests', controller.signal),
        apiClient.get<MatchedRequest[]>('/api/matches', controller.signal),
        apiClient.get<NotificationItem[]>('/api/notifications', controller.signal),
      ])
        .then(([requests, matchResults, notificationResults]) => {
          setMyRequests(requests)
          setMatches(matchResults)
          setNotifications(notificationResults)
        })
        .catch((err: unknown) => {
          if (!controller.signal.aborted) {
            setError(err instanceof ApiError ? err.message : 'Failed to load your dashboard.')
          }
        })
    })

    return () => controller.abort()
  }, [apiClient])

  if (error) {
    return <ErrorState message={error} />
  }

  if (!myRequests || !matches || !notifications) {
    return <LoadingState label="Loading your dashboard…" />
  }

  const openRequests = myRequests.filter((r) => r.status === 'Open')
  const pendingMatches = matches.filter((m) => m.myResponseStatus === 'Pending')
  const unreadNotifications = notifications.filter((n) => !n.readByRecipient)

  return (
    <div className="page">
      <h1>Welcome back, {currentUser?.displayName.split(' ')[0]}</h1>
      <p className="page__subtitle">
        Here's what's happening in your employee blood donor network right now.
      </p>

      <div className="stat-grid">
        <div className="card stat-card">
          <span className="stat-card__value">{openRequests.length}</span>
          <span className="stat-card__label">Open requests you created</span>
          <Link to="/requests" className="btn btn--secondary">
            View requests
          </Link>
        </div>
        <div className="card stat-card">
          <span className="stat-card__value">{pendingMatches.length}</span>
          <span className="stat-card__label">Matched requests awaiting your response</span>
          <Link to="/matches" className="btn btn--secondary">
            Respond now
          </Link>
        </div>
        <div className="card stat-card">
          <span className="stat-card__value">{unreadNotifications.length}</span>
          <span className="stat-card__label">Unread notifications</span>
          <Link to="/notifications" className="btn btn--secondary">
            Open inbox
          </Link>
        </div>
      </div>

      <div className="card">
        <div className="card__header">
          <h2>Need blood for a patient?</h2>
          <Link to="/requests/new" className="btn btn--primary">
            Create a blood request
          </Link>
        </div>
        <p>
          Requests are matched automatically against opted-in donors by blood group compatibility,
          availability, and medical eligibility — only matched donors are notified.
        </p>
      </div>

      {pendingMatches.length > 0 && (
        <div className="card">
          <h2>Requests matched to you</h2>
          <ul className="request-list">
            {pendingMatches.slice(0, 5).map((match) => (
              <li key={match.responseId} className="request-list__item">
                <div>
                  <strong>{formatBloodGroup(match.bloodGroup)}</strong> needed at {match.hospitalName},{' '}
                  {match.city} ({match.unitsNeeded} unit{match.unitsNeeded === 1 ? '' : 's'})
                </div>
                <Link to="/matches" className="btn btn--secondary">
                  Respond
                </Link>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
