namespace BloodConnect.Domain;

/// <summary>
/// A request for blood, created by an employee on behalf of a patient (self, family member, or colleague).
/// </summary>
public class BloodRequest
{
    public Guid Id { get; set; }

    public Guid RequesterId { get; set; }

    public AppUser? Requester { get; set; }

    public BloodGroup BloodGroup { get; set; }

    public string HospitalName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code for pincode-based location proximity matching.
    /// Currently used for Delhi; will be extended to other cities.
    /// </summary>
    public string? Pincode { get; set; }

    public int UnitsNeeded { get; set; }

    public UrgencyLevel Urgency { get; set; }

    public string? Notes { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Open;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Requests are automatically considered expired after this UTC instant if still open,
    /// so stale critical requests don't linger indefinitely in dashboards.
    /// </summary>
    public DateTime ExpiresUtc { get; set; }

    public List<DonorResponse> Responses { get; set; } = new();
}
