namespace BloodConnect.Domain;

/// <summary>
/// A durable, observable record of a notification that BloodConnect wanted to deliver. Every
/// notification (Teams, email, in-app) is written here first (the "outbox" pattern) so delivery
/// can be retried/audited and the local/demo UI can show a real notification inbox without any
/// external service credentials.
/// </summary>
public class NotificationOutboxItem
{
    public Guid Id { get; set; }

    public Guid RecipientUserId { get; set; }

    public AppUser? RecipientUser { get; set; }

    public NotificationType Type { get; set; }

    public Guid? BloodRequestId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Adaptive Card JSON payload for Teams delivery. Present even for local delivery so the demo
    /// UI can render exactly what would be sent to Teams.
    /// </summary>
    public string AdaptiveCardJson { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public DateTime CreatedUtc { get; set; }

    public DateTime? SentUtc { get; set; }

    public bool ReadByRecipient { get; set; }

    /// <summary>
    /// Honest record of which delivery channel actually attempted to send this notification
    /// ("Local" or "TeamsWebhook"), and any failure detail if delivery could not be confirmed.
    /// </summary>
    public string Channel { get; set; } = "Local";

    public string? DeliveryDetail { get; set; }
}
