import type { BloodRequest, ResponseStatus } from '../api/types'

const REQUEST_STATUS_CLASS: Record<BloodRequest['status'], string> = {
  Open: 'badge--success',
  Fulfilled: 'badge--info',
  Cancelled: 'badge--muted',
  Expired: 'badge--warning',
}

const RESPONSE_STATUS_CLASS: Record<ResponseStatus, string> = {
  Pending: 'badge--muted',
  Available: 'badge--success',
  NotAvailable: 'badge--warning',
}

export function StatusBadge({ status }: { status: BloodRequest['status'] }) {
  return <span className={`badge ${REQUEST_STATUS_CLASS[status]}`}>{status}</span>
}

export function ResponseStatusBadge({ status }: { status: ResponseStatus }) {
  const label = status === 'NotAvailable' ? 'Not available' : status
  return <span className={`badge ${RESPONSE_STATUS_CLASS[status]}`}>{label}</span>
}
