namespace BloodConnect.Domain;

/// <summary>
/// A private, opt-in donor profile owned by exactly one user. Never returned in bulk to other users;
/// only surfaced to a requester after the donor affirmatively responds "Available" to a specific request.
/// </summary>
public class DonorProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AppUser? User { get; set; }

    public BloodGroup BloodGroup { get; set; }

    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code for pincode-based location proximity matching.
    /// Currently used for Delhi; will be extended to other cities.
    /// </summary>
    public string? Pincode { get; set; }

    public AvailabilityStatus Availability { get; set; } = AvailabilityStatus.Available;

    /// <summary>
    /// UTC date of the donor's last completed donation, if any. Used to compute medical eligibility.
    /// </summary>
    public DateTime? LastDonationUtc { get; set; }

    public ContactPreference ContactPreference { get; set; } = ContactPreference.TeamsChat;

    public string? ContactPhone { get; set; }

    /// <summary>
    /// True once the employee has explicitly opted in to appear in matching. Profiles are only
    /// ever created via this opt-in flow, but the flag makes intent explicit and auditable.
    /// </summary>
    public bool HasOptedIn { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public List<DonationHistoryEntry> DonationHistory { get; set; } = new();
}
