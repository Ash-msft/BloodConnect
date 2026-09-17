using BloodConnect.Domain;

namespace BloodConnect.Api.Dtos;

public record CurrentUserDto(Guid UserId, string DisplayName, string Email, bool HasDonorProfile);

public record DonorProfileDto(
    BloodGroup BloodGroup,
    string City,
    string? Pincode,
    AvailabilityStatus Availability,
    DateTime? LastDonationUtc,
    ContactPreference ContactPreference,
    string? ContactPhone,
    bool HasOptedIn,
    bool IsEligibleNow,
    DateTime? NextEligibleUtc,
    string? LocationZone);

public record UpsertDonorProfileRequest(
    BloodGroup BloodGroup,
    string City,
    AvailabilityStatus Availability,
    DateTime? LastDonationUtc,
    ContactPreference ContactPreference,
    string? ContactPhone,
    bool HasOptedIn,
    string? Pincode = null);

public record DonationHistoryDto(Guid Id, DateTime DonationDateUtc, string? Notes, Guid? FulfilledRequestId);

public record RecordDonationRequest(DateTime DonationDateUtc, string? Notes, Guid? FulfilledRequestId);

public record CreateBloodRequestDto(
    BloodGroup BloodGroup,
    string HospitalName,
    string City,
    int UnitsNeeded,
    UrgencyLevel Urgency,
    string? Notes,
    string? Pincode = null);

public record BloodRequestDto(
    Guid Id,
    BloodGroup BloodGroup,
    string HospitalName,
    string City,
    string? Pincode,
    int UnitsNeeded,
    UrgencyLevel Urgency,
    string? Notes,
    RequestStatus Status,
    DateTime CreatedUtc,
    DateTime ExpiresUtc,
    int MatchedDonorCount,
    int AvailableDonorCount,
    bool IsOwnRequest,
    string? LocationZone,
    double SearchRadiusKm);

/// <summary>
/// A donor response as seen by the requester. Identity/contact fields are null unless the donor has
/// affirmed availability — this is enforced server-side regardless of what the client requests.
/// </summary>
public record DonorResponseForRequesterDto(
    Guid ResponseId,
    ResponseStatus Status,
    int ProximityRank,
    DateTime NotifiedUtc,
    DateTime? RespondedUtc,
    string? DonorDisplayName,
    string? DonorEmail,
    string? DonorPhone,
    ContactPreference? DonorContactPreference);

/// <summary>
/// A request as seen by a matched donor, along with their own pending/answered response.
/// </summary>
public record MatchedRequestDto(
    Guid RequestId,
    BloodGroup BloodGroup,
    string HospitalName,
    string City,
    int UnitsNeeded,
    UrgencyLevel Urgency,
    string? Notes,
    ResponseStatus MyResponseStatus,
    Guid ResponseId,
    DateTime NotifiedUtc);

public record RespondToRequestDto(ResponseStatus Response);

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    Guid? BloodRequestId,
    string Title,
    string Body,
    string AdaptiveCardJson,
    NotificationStatus Status,
    string Channel,
    string? DeliveryDetail,
    DateTime CreatedUtc,
    bool ReadByRecipient);

public record DemoUserOptionDto(string ExternalId, string DisplayName);

public record ApiErrorResponse(string Message);
