namespace BloodConnect.Domain;

/// <summary>
/// Records a matched donor's response to a specific blood request. The requester may only see
/// the donor's identity/contact details once <see cref="Status"/> is <see cref="ResponseStatus.Available"/>.
/// </summary>
public class DonorResponse
{
    public Guid Id { get; set; }

    public Guid BloodRequestId { get; set; }

    public BloodRequest? BloodRequest { get; set; }

    public Guid DonorUserId { get; set; }

    public AppUser? DonorUser { get; set; }

    public ResponseStatus Status { get; set; } = ResponseStatus.Pending;

    /// <summary>
    /// Distance ranking snapshot (same city = 0, otherwise higher) captured at match time, used to sort
    /// donor lists and included so the requester can see why a donor was prioritized.
    /// </summary>
    public int ProximityRank { get; set; }

    public DateTime NotifiedUtc { get; set; }

    public DateTime? RespondedUtc { get; set; }
}
