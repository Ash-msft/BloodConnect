import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { ApiError } from '../api/client'
import type { BloodRequest, DonorResponseForRequester } from '../api/types'
import { formatBloodGroup } from '../api/types'
import { EmptyState, ErrorState, LoadingState } from '../components/StatusStates'
import { ResponseStatusBadge, StatusBadge } from '../components/StatusBadge'
import { formatDateTime } from '../utils/eligibility'

export function RequestDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { apiClient } = useAuth()
  const [request, setRequest] = useState<BloodRequest | null>(null)
  const [responses, setResponses] = useState<DonorResponseForRequester[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [isActing, setIsActing] = useState(false)

  const load = useCallback(
    (signal?: AbortSignal) => {
      if (!id) return
      setError(null)
      Promise.all([
        apiClient.get<BloodRequest>(`/api/requests/${id}`, signal),
        apiClient.get<DonorResponseForRequester[]>(`/api/requests/${id}/responses`, signal),
      ])
        .then(([requestResult, responseResults]) => {
          setRequest(requestResult)
          setResponses(responseResults)
        })
        .catch((err: unknown) => {
          if (!signal?.aborted) {
            setError(err instanceof ApiError ? err.message : 'Failed to load this request.')
          }
        })
    },
    [apiClient, id],
  )

  useEffect(() => {
    const controller = new AbortController()
    Promise.resolve().then(() => load(controller.signal))
    return () => controller.abort()
  }, [load])

  async function handleCancel() {
    if (!id) return
    setIsActing(true)
    setActionError(null)
    try {
      const updated = await apiClient.post<BloodRequest>(`/api/requests/${id}/cancel`)
      setRequest(updated)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Failed to cancel the request.')
    } finally {
      setIsActing(false)
    }
  }

  async function handleFulfill() {
    if (!id) return
    setIsActing(true)
    setActionError(null)
    try {
      const updated = await apiClient.post<BloodRequest>(`/api/requests/${id}/fulfill`)
      setRequest(updated)
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Failed to mark the request fulfilled.')
    } finally {
      setIsActing(false)
    }
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => load()} />
  }

  if (!request || !responses) {
    return <LoadingState label="Loading request details…" />
  }

  const canManage = request.isOwnRequest && (request.status === 'Open')

  return (
    <div className="page">
      <div className="page__header">
        <h1>
          {formatBloodGroup(request.bloodGroup)} needed at {request.hospitalName}
        </h1>
        <StatusBadge status={request.status} />
      </div>

      <div className="card">
        <dl className="detail-grid">
          <div>
            <dt>City</dt>
            <dd>{request.city}</dd>
          </div>
          <div>
            <dt>Units needed</dt>
            <dd>{request.unitsNeeded}</dd>
          </div>
          <div>
            <dt>Urgency</dt>
            <dd>{request.urgency}</dd>
          </div>
          <div>
            <dt>Created</dt>
            <dd>{formatDateTime(request.createdUtc)}</dd>
          </div>
          <div>
            <dt>Expires</dt>
            <dd>{formatDateTime(request.expiresUtc)}</dd>
          </div>
        </dl>
        {request.notes && <p className="request-notes">{request.notes}</p>}

        {canManage && (
          <div className="button-row">
            <button type="button" className="btn btn--primary" onClick={handleFulfill} disabled={isActing}>
              Mark fulfilled
            </button>
            <button type="button" className="btn btn--danger" onClick={handleCancel} disabled={isActing}>
              Cancel request
            </button>
          </div>
        )}
        {actionError && (
          <p className="form-error" role="alert">
            {actionError}
          </p>
        )}
      </div>

      <div className="card">
        <h2>Matched donor responses</h2>
        <p className="disclaimer-text">
          Donor identity and contact details are only shown once a donor affirms availability for this
          request. Donors who haven't responded, or who declined, remain anonymous.
        </p>
        {responses.length === 0 ? (
          <EmptyState
            title="No donors have been matched yet."
            description="This can happen if no opted-in, available, eligible donors currently match this blood group."
          />
        ) : (
          <ul className="response-list">
            {responses.map((response) => (
              <li key={response.responseId} className="response-list__item">
                <ResponseStatusBadge status={response.status} />
                {response.status === 'Available' ? (
                  <div>
                    <strong>{response.donorDisplayName}</strong>
                    <div>{response.donorEmail}</div>
                    {response.donorPhone && <div>{response.donorPhone}</div>}
                    <div className="request-list__meta">
                      Preferred contact: {response.donorContactPreference}
                    </div>
                  </div>
                ) : (
                  <div className="request-list__meta">
                    {response.status === 'Pending'
                      ? 'Awaiting response'
                      : 'Donor declined — identity not shared'}
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
