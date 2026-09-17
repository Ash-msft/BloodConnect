import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '../context/useAuth'
import { ApiError } from '../api/client'
import type { NotificationItem } from '../api/types'
import { EmptyState, ErrorState, LoadingState } from '../components/StatusStates'
import { formatDateTime } from '../utils/eligibility'

const CHANNEL_LABEL: Record<string, string> = {
  Local: 'Local in-app inbox',
  TeamsWebhook: 'Microsoft Teams',
}

export function NotificationsPage() {
  const { apiClient } = useAuth()
  const [notifications, setNotifications] = useState<NotificationItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(
    (signal?: AbortSignal) => {
      setError(null)
      apiClient
        .get<NotificationItem[]>('/api/notifications', signal)
        .then(setNotifications)
        .catch((err: unknown) => {
          if (!signal?.aborted) {
            setError(err instanceof ApiError ? err.message : 'Failed to load your notifications.')
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

  async function markRead(id: string) {
    try {
      await apiClient.post(`/api/notifications/${id}/read`)
      setNotifications((prev) => prev?.map((n) => (n.id === id ? { ...n, readByRecipient: true } : n)) ?? null)
    } catch {
      // Marking as read is a non-critical convenience action; a failure here doesn't block reading
      // the notification body, so we don't surface a blocking error for it.
    }
  }

  if (error) {
    return <ErrorState message={error} onRetry={() => load()} />
  }

  if (!notifications) {
    return <LoadingState label="Loading your notifications…" />
  }

  return (
    <div className="page">
      <h1>Notification Inbox</h1>
      <p className="page__subtitle">
        Every notification BloodConnect wants to deliver to you is recorded here, regardless of
        delivery channel — this is the durable outbox record, made observable for transparency.
      </p>

      {notifications.length === 0 ? (
        <EmptyState title="No notifications yet." />
      ) : (
        <ul className="notification-list">
          {notifications.map((notification) => (
            <li
              key={notification.id}
              className={`card notification-item ${notification.readByRecipient ? '' : 'notification-item--unread'}`}
            >
              <div className="notification-item__header">
                <strong>{notification.title}</strong>
                <span className={`badge ${notification.status === 'Sent' ? 'badge--success' : 'badge--warning'}`}>
                  {notification.status}
                </span>
              </div>
              <p>{notification.body}</p>
              <div className="request-list__meta">
                {CHANNEL_LABEL[notification.channel] ?? notification.channel} ·{' '}
                {formatDateTime(notification.createdUtc)}
              </div>
              {notification.deliveryDetail && (
                <p className="disclaimer-text">{notification.deliveryDetail}</p>
              )}
              {!notification.readByRecipient && (
                <button type="button" className="btn btn--secondary" onClick={() => markRead(notification.id)}>
                  Mark as read
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
