namespace BloodConnect.Infrastructure.Options;

/// <summary>
/// Application-wide configurable options, bound from the "BloodConnect" configuration section.
/// </summary>
public class BloodConnectOptions
{
    public const string SectionName = "BloodConnect";

    /// <summary>
    /// Minimum number of days required between whole-blood donations before a donor is considered
    /// medically eligible again. Configurable per README guidance; default matches common whole-blood
    /// donation intervals used by many donation centers. This value is informational only.
    /// </summary>
    public int MinimumDonationIntervalDays { get; set; } = Domain.BloodEligibilityCalculator.DefaultMinimumIntervalDays;

    /// <summary>
    /// Number of hours an open blood request remains active before being automatically expired.
    /// </summary>
    public int RequestExpiryHours { get; set; } = 72;

    /// <summary>
    /// Maximum number of donors notified per new blood request, to avoid notification fatigue.
    /// </summary>
    public int MaxDonorsNotifiedPerRequest { get; set; } = 50;
}
