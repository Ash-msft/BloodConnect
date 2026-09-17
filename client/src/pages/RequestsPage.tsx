import { Link } from 'react-router-dom'
import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../context/useAuth'
import { ApiError } from '../api/client'
import type { BloodRequest } from '../api/types'
import { formatBloodGroup } from '../api/types'
import { EmptyState, ErrorState, LoadingState } from '../components/StatusStates'
import { StatusBadge } from '../components/StatusBadge'
import { formatDateTime } from '../utils/eligibility'

export function RequestsPage() {
  const { apiClient } = useAuth()
  const [requests, setRequests] = useState<BloodRequest[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(
    (signal?: AbortSignal) => {
      setError(null)
      apiClient
        .get<BloodRequest[]>('/api/requests', signal)
        .then(setRequests)
        .catch((err: unknown) => {
          if (!signal?.aborted) {
            setError(err instanceof ApiError ? err.message : 'Failed to load your requests.')
          }
        })
    },
    [apiClient],
  )

  useEffect(() => {
    const controller = new AbortController()
    Promise.resolve().then(() => load(controller.signal))
    return () => controller.abort()
  }, [load])

  if (error) {
    return <ErrorState message={error} onRetry={() => load()} />
  }

  if (!requests) {
    return <LoadingState label="Loading your requests…" />
  }

  return (
    <div className="page">
      <div className="page__header">
        <h1>My Blood Requests</h1>
        <Link to="/requests/new" className="btn btn--primary">
          Create a blood request
        </Link>
      </div>

      {requests.length === 0 ? (
        <EmptyState
          title="You haven't created any blood requests yet."
          description="Create one to notify compatible, available, and eligible donors."
        />
      ) : (
        <ul className="request-list">
          {requests.map((request) => (
            <li key={request.id} className="request-list__item card">
              <div>
                <strong>{formatBloodGroup(request.bloodGroup)}</strong> at {request.hospitalName},{' '}
                {request.city}
                <div className="request-list__meta">
                  {request.unitsNeeded} unit{request.unitsNeeded === 1 ? '' : 's'} · {request.urgency} ·{' '}
                  Created {formatDateTime(request.createdUtc)}
                </div>
              </div>
              <div className="request-list__actions">
                <StatusBadge status={request.status} />
                <span className="badge badge--muted">
                  {request.availableDonorCount}/{request.matchedDonorCount} donors available
                </span>
                <Link to={`/requests/${request.id}`} className="btn btn--secondary">
                  View details
                </Link>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
