namespace BloodConnect.Domain;

/// <summary>
/// Computes whole-blood donation eligibility based on the configurable minimum interval between
/// donations (default 56 days, matching common whole-blood donation guidance). This is informational
/// only — actual eligibility (hemoglobin levels, health screening, travel history, etc.) is always
/// determined by the donation center at the time of donation.
/// </summary>
public static class BloodEligibilityCalculator
{
    public const int DefaultMinimumIntervalDays = 56;

    /// <summary>
    /// Returns true if a donor whose last donation was on <paramref name="lastDonationUtc"/> (or who has
    /// never donated, if null) is eligible to donate again as of <paramref name="asOfUtc"/>.
    /// </summary>
    public static bool IsEligible(DateTime? lastDonationUtc, DateTime asOfUtc, int minimumIntervalDays = DefaultMinimumIntervalDays)
    {
        if (minimumIntervalDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumIntervalDays), "Minimum interval days cannot be negative.");
        }

        if (lastDonationUtc is null)
        {
            return true;
        }

        var nextEligibleUtc = lastDonationUtc.Value.AddDays(minimumIntervalDays);
        return asOfUtc >= nextEligibleUtc;
    }

    /// <summary>
    /// Returns the UTC date/time at which a donor becomes eligible again, or null if they are
    /// already eligible (including donors who have never donated).
    /// </summary>
    public static DateTime? GetNextEligibleUtc(DateTime? lastDonationUtc, int minimumIntervalDays = DefaultMinimumIntervalDays)
    {
        if (lastDonationUtc is null)
        {
            return null;
        }

        return lastDonationUtc.Value.AddDays(minimumIntervalDays);
    }
}
