using BloodConnect.Domain;
using Microsoft.Extensions.Logging;

namespace BloodConnect.Infrastructure.Notifications;

/// <summary>
/// Local development delivery channel. Marks the outbox item as "Sent" immediately since delivery is
/// simply making it visible via the in-app notification inbox/API — there is no external transport,
/// and this class makes no claim otherwise.
/// </summary>
public class LocalNotificationChannel : INotificationChannel
{
    private readonly ILogger<LocalNotificationChannel> _logger;

    public LocalNotificationChannel(ILogger<LocalNotificationChannel> logger)
    {
        _logger = logger;
    }

    public string Name => "Local";

    public Task DeliverAsync(NotificationOutboxItem item, CancellationToken cancellationToken)
    {
        item.Status = NotificationStatus.Sent;
        item.SentUtc = DateTime.UtcNow;
        item.DeliveryDetail = "Delivered to in-app notification inbox only (no external channel configured).";
        _logger.LogInformation(
            "Local notification recorded for user {RecipientUserId}: {Title}", item.RecipientUserId, item.Title);
        return Task.CompletedTask;
    }
}
