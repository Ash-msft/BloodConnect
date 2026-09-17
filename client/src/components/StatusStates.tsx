export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="status-state status-state--loading" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="status-state status-state--error" role="alert">
      <p>{message}</p>
      {onRetry && (
        <button type="button" className="btn btn--secondary" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="status-state status-state--empty">
      <p className="status-state__title">{title}</p>
      {description && <p>{description}</p>}
    </div>
  )
}
