namespace BloodConnect.Infrastructure.Notifications;

/// <summary>
/// Delivers a single already-persisted outbox notification. Implementations must never throw for
/// expected delivery failures; they should update the item's status/detail and return normally so
/// the caller can decide how to surface failures (no silent swallowing, no broad catches upstream).
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// Unique channel name, stored on the outbox item for auditability.
    /// </summary>
    string Name { get; }

    Task DeliverAsync(Domain.NotificationOutboxItem item, CancellationToken cancellationToken);
}
