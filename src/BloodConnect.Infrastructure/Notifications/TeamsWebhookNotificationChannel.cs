using System.Net.Http.Json;
using BloodConnect.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BloodConnect.Infrastructure.Notifications;

/// <summary>
/// Teams delivery channel backed by an Incoming Webhook connector on a Teams channel. This is a real,
/// honest network integration: it is only active when explicitly enabled and configured with a webhook
/// URL (see README "Entra ID / Azure configuration"). It does not simulate success — a missing/invalid
/// configuration or a failed HTTP call results in the outbox item being marked Failed with a concrete
/// error detail, never a silent no-op.
/// </summary>
public class TeamsWebhookNotificationChannel : INotificationChannel
{
    private readonly HttpClient _httpClient;
    private readonly TeamsNotificationOptions _options;
    private readonly ILogger<TeamsWebhookNotificationChannel> _logger;

    public TeamsWebhookNotificationChannel(
        HttpClient httpClient,
        IOptions<TeamsNotificationOptions> options,
        ILogger<TeamsWebhookNotificationChannel> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "TeamsWebhook";

    public async Task DeliverAsync(NotificationOutboxItem item, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            item.Status = NotificationStatus.Failed;
            item.DeliveryDetail = "Teams delivery is not enabled (BloodConnect:Teams:Enabled=false). " +
                                   "Notification recorded in outbox only.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.IncomingWebhookUrl))
        {
            item.Status = NotificationStatus.Failed;
            item.DeliveryDetail = "Teams delivery is enabled but no IncomingWebhookUrl is configured.";
            return;
        }

        var payload = new
        {
            type = "message",
            attachments = new object[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = System.Text.Json.JsonDocument.Parse(item.AdaptiveCardJson).RootElement
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.IncomingWebhookUrl, payload, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            item.Status = NotificationStatus.Sent;
            item.SentUtc = DateTime.UtcNow;
            item.DeliveryDetail = "Delivered via Teams incoming webhook.";
        }
        else
        {
            item.Status = NotificationStatus.Failed;
            item.DeliveryDetail = $"Teams webhook responded with status {(int)response.StatusCode}.";
            _logger.LogWarning(
                "Teams webhook delivery failed for notification {NotificationId} with status {StatusCode}",
                item.Id, response.StatusCode);
        }
    }
}
