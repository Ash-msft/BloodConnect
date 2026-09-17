using BloodConnect.Domain;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Infrastructure.Notifications;

/// <summary>
/// Creates durable outbox notification records and dispatches them through the active
/// <see cref="INotificationChannel"/>. Selecting the channel is configuration-driven
/// (BloodConnect:Teams:Enabled) rather than hard-coded, matching the "honest adapter" requirement.
/// </summary>
public class NotificationService
{
    private readonly BloodConnectDbContext _db;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly TeamsNotificationOptions _teamsOptions;

    public NotificationService(
        BloodConnectDbContext db,
        IEnumerable<INotificationChannel> channels,
        Microsoft.Extensions.Options.IOptions<TeamsNotificationOptions> teamsOptions)
    {
        _db = db;
        _channels = channels;
        _teamsOptions = teamsOptions.Value;
    }

    public async Task<NotificationOutboxItem> CreateAndDeliverAsync(
        Guid recipientUserId,
        NotificationType type,
        Guid? bloodRequestId,
        string title,
        string body,
        string adaptiveCardJson,
        CancellationToken cancellationToken)
    {
        var channelName = _teamsOptions.Enabled ? "TeamsWebhook" : "Local";
        var channel = _channels.FirstOrDefault(c => c.Name == channelName)
            ?? throw new InvalidOperationException($"No notification channel registered for '{channelName}'.");

        var item = new NotificationOutboxItem
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Type = type,
            BloodRequestId = bloodRequestId,
            Title = title,
            Body = body,
            AdaptiveCardJson = adaptiveCardJson,
            Status = NotificationStatus.Pending,
            CreatedUtc = DateTime.UtcNow,
            Channel = channelName
        };

        _db.NotificationOutboxItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        await channel.DeliverAsync(item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return item;
    }

    public async Task<List<NotificationOutboxItem>> GetInboxAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.NotificationOutboxItems
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var item = await _db.NotificationOutboxItems
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId, cancellationToken);

        if (item is null)
        {
            return false;
        }

        item.ReadByRecipient = true;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
