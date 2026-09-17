import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../context/useAuth'
import { ApiError } from '../api/client'
import type { MatchedRequest, ResponseStatus } from '../api/types'
import { formatBloodGroup } from '../api/types'
import { EmptyState, ErrorState, LoadingState } from '../components/StatusStates'
import { ResponseStatusBadge } from '../components/StatusBadge'
import { formatDateTime } from '../utils/eligibility'

export function MatchesPage() {
  const { apiClient } = useAuth()
  const [matches, setMatches] = useState<MatchedRequest[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [respondingId, setRespondingId] = useState<string | null>(null)
  const [respondError, setRespondError] = useState<string | null>(null)

  const load = useCallback(
    (signal?: AbortSignal) => {
      setError(null)
      apiClient
        .get<MatchedRequest[]>('/api/matches', signal)
        .then(setMatches)
        .catch((err: unknown) => {
          if (!signal?.aborted) {
            setError(err instanceof ApiError ? err.message : 'Failed to load your matched requests.')
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

  async function respond(responseId: string, response: ResponseStatus) {
    setRespondingId(responseId)
    setRespondError(null)
    try {
      const updated = await apiClient.post<MatchedRequest>(`/api/matches/${responseId}/respond`, {
        response,
      })
      setMatches((prev) => prev?.map((m) => (m.responseId === responseId ? updated : m)) ?? null)
    } catch (err) {
      setRespondError(err instanceof ApiError ? err.message : 'Failed to submit your response.')
    } finally {
      setRespondingId(null)
    }
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => load()} />
  }

  if (!matches) {
    return <LoadingState label="Loading matched requests…" />
  }

  return (
    <div className="page">
      <h1>Requests Matched to You</h1>
      <p className="page__subtitle">
        You only see requests that matched your donor profile's blood group, availability, and
        eligibility. Your response is only shared with the requester if you choose "I'm available".
      </p>

      {respondError && (
        <p className="form-error" role="alert">
          {respondError}
        </p>
      )}

      {matches.length === 0 ? (
        <EmptyState
          title="No matched requests right now."
          description="You'll see a request here if your donor profile matches a colleague's blood request."
        />
      ) : (
        <ul className="request-list">
          {matches.map((match) => (
            <li key={match.responseId} className="request-list__item card">
              <div>
                <strong>{formatBloodGroup(match.bloodGroup)}</strong> needed at {match.hospitalName},{' '}
                {match.city}
                <div className="request-list__meta">
                  {match.unitsNeeded} unit{match.unitsNeeded === 1 ? '' : 's'} · {match.urgency} ·
                  Notified {formatDateTime(match.notifiedUtc)}
                </div>
                {match.notes && <p className="request-notes">{match.notes}</p>}
              </div>
              <div className="request-list__actions">
                <ResponseStatusBadge status={match.myResponseStatus} />
                {match.myResponseStatus === 'Pending' && (
                  <>
                    <button
                      type="button"
                      className="btn btn--primary"
                      disabled={respondingId === match.responseId}
                      onClick={() => respond(match.responseId, 'Available')}
                    >
                      I'm available
                    </button>
                    <button
                      type="button"
                      className="btn btn--secondary"
                      disabled={respondingId === match.responseId}
                      onClick={() => respond(match.responseId, 'NotAvailable')}
                    >
                      Not available
                    </button>
                  </>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
