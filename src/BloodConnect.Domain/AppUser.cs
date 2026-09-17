namespace BloodConnect.Domain;

/// <summary>
/// An employee/application user. In local development this is one of a fixed set of demo
/// identities selected client-side and validated/mapped server-side (see DemoAuthenticationHandler).
/// In production this maps 1:1 to an authenticated Entra ID object id (see README for the migration path).
/// </summary>
public class AppUser
{
    public Guid Id { get; set; }

    /// <summary>
    /// Stable external identity key. For local demo auth this is a well-known demo user id
    /// (e.g. "demo-priya"). For Entra ID this would be the token's `oid` claim.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DonorProfile? DonorProfile { get; set; }
}
