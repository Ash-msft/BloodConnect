namespace BloodConnect.Infrastructure.Notifications;

/// <summary>
/// Configuration for the Teams delivery channel. When <see cref="Enabled"/> is false (the default),
/// notifications are recorded in the outbox only and are visible via the in-app notification inbox/API —
/// this is the honest local/demo behavior with zero external dependencies. When enabled, an incoming
/// webhook URL (or, for a production bot, additional bot credentials) must be supplied; see README for
/// the full Entra ID / Azure Bot Service configuration path required to actually deliver to Teams.
/// </summary>
public class TeamsNotificationOptions
{
    public const string SectionName = "Teams";

    public bool Enabled { get; set; }

    /// <summary>
    /// Incoming webhook URL for a Teams channel connector. Only used when <see cref="Enabled"/> is true.
    /// Never commit a real value; supply via user secrets/environment/Azure configuration.
    /// </summary>
    public string? IncomingWebhookUrl { get; set; }

    /// <summary>
    /// Base URL of the deployed BloodConnect web app, used to build Adaptive Card action URLs
    /// (e.g. deep links back into the personal tab) that are only meaningful once actually hosted.
    /// </summary>
    public string? AppBaseUrl { get; set; }
}
