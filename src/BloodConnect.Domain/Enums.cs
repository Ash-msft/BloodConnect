namespace BloodConnect.Domain;

/// <summary>
/// All standard ABO/Rh blood group combinations.
/// </summary>
public enum BloodGroup
{
    OPositive,
    ONegative,
    APositive,
    ANegative,
    BPositive,
    BNegative,
    ABPositive,
    ABNegative
}

/// <summary>
/// Whether a donor currently considers themselves available to be contacted for donation.
/// This is independent of medical eligibility, which is computed from donation history.
/// </summary>
public enum AvailabilityStatus
{
    Available,
    Unavailable
}

/// <summary>
/// Donor's preferred channel for being contacted once they affirm availability for a specific request.
/// </summary>
public enum ContactPreference
{
    TeamsChat,
    Email,
    Phone
}

/// <summary>
/// Urgency level attached to a blood request, used for sorting/prioritization and notification framing.
/// </summary>
public enum UrgencyLevel
{
    Routine,
    Urgent,
    Critical
}

/// <summary>
/// Lifecycle state of a blood request.
/// </summary>
public enum RequestStatus
{
    Open,
    Fulfilled,
    Cancelled,
    Expired
}

/// <summary>
/// A donor's response to a specific matched blood request notification.
/// </summary>
public enum ResponseStatus
{
    Pending,
    Available,
    NotAvailable
}

/// <summary>
/// Delivery status of an outbox notification record.
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

/// <summary>
/// The kind of notification event being recorded, used to select rendering/template logic.
/// </summary>
public enum NotificationType
{
    NewMatchingRequest,
    DonorAvailableResponse,
    RequestFulfilled,
    RequestCancelled
}
