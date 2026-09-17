namespace BloodConnect.Domain;

/// <summary>
/// A record of a completed donation, appended to a donor's history. Updating history recomputes
/// medical eligibility for future matching (see BloodEligibilityCalculator).
/// </summary>
public class DonationHistoryEntry
{
    public Guid Id { get; set; }

    public Guid DonorProfileId { get; set; }

    public DonorProfile? DonorProfile { get; set; }

    public DateTime DonationDateUtc { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Optional link to the request this donation fulfilled, if it originated from BloodConnect.
    /// </summary>
    public Guid? FulfilledRequestId { get; set; }

    public DateTime RecordedUtc { get; set; }
}
