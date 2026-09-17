using BloodConnect.Domain;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Infrastructure.Seed;

/// <summary>
/// Seeds realistic demo users, donor profiles, and a couple of sample requests. Intended to run only
/// in the Development environment (see Program.cs), so demo data never appears in a real deployment.
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>
    /// Well-known demo identities. These map 1:1 to the local demo authentication scheme's user
    /// selector in the frontend; see DemoAuthenticationHandler for how a request header is validated
    /// against this same set server-side.
    /// 
    /// Updated to include Delhi-based donors with pincodes for proximity matching.
    /// </summary>
    public static readonly (string ExternalId, string DisplayName, string Email)[] DemoUsers =
    {
        ("demo-priya", "Priya Sharma", "priya.sharma@contoso.demo"),
        ("demo-arjun", "Arjun Mehta", "arjun.mehta@contoso.demo"),
        ("demo-fatima", "Fatima Khan", "fatima.khan@contoso.demo"),
        ("demo-liam", "Liam O'Connor", "liam.oconnor@contoso.demo"),
        ("demo-wei", "Wei Zhang", "wei.zhang@contoso.demo"),
        ("demo-sofia", "Sofia Rossi", "sofia.rossi@contoso.demo"),
        ("demo-noah", "Noah Williams", "noah.williams@contoso.demo"),
        ("demo-ana", "Ana Costa", "ana.costa@contoso.demo"),
        // Additional Delhi-based donors for pincode testing
        ("demo-rajesh", "Rajesh Singh", "rajesh.singh@contoso.demo"),
        ("demo-deepika", "Deepika Patel", "deepika.patel@contoso.demo"),
        ("demo-anil", "Anil Kumar", "anil.kumar@contoso.demo"),
        ("demo-neha", "Neha Gupta", "neha.gupta@contoso.demo")
    };

    public static async Task SeedAsync(BloodConnectDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var users = new List<AppUser>();

        foreach (var (externalId, displayName, email) in DemoUsers)
        {
            users.Add(new AppUser
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                DisplayName = displayName,
                Email = email
            });
        }

        db.Users.AddRange(users);

        // Mix of original demo data + Delhi-based donors with pincodes
        var profiles = new List<DonorProfile>
        {
            // Original demo users (for backward compatibility, no pincodes)
            Profile(users[0], BloodGroup.OPositive, "Bengaluru", null, AvailabilityStatus.Available, now.AddDays(-90)),
            Profile(users[1], BloodGroup.ONegative, "Bengaluru", null, AvailabilityStatus.Available, null),
            Profile(users[2], BloodGroup.APositive, "Hyderabad", null, AvailabilityStatus.Available, now.AddDays(-20)),
            Profile(users[3], BloodGroup.BNegative, "Dublin", null, AvailabilityStatus.Unavailable, now.AddDays(-200)),
            Profile(users[4], BloodGroup.ABPositive, "Bengaluru", null, AvailabilityStatus.Available, now.AddDays(-10)),
            Profile(users[5], BloodGroup.APositive, "Milan", null, AvailabilityStatus.Available, null),

            // New Delhi-based donors with pincodes for proximity matching tests
            // Central Delhi Zone
            Profile(users[6], BloodGroup.OPositive, "Delhi", "110001", AvailabilityStatus.Available, now.AddDays(-30)),

            // East Delhi Zone  
            Profile(users[7], BloodGroup.APositive, "Delhi", "110018", AvailabilityStatus.Available, now.AddDays(-45)),

            // South Delhi Zone
            Profile(users[8], BloodGroup.BPositive, "Delhi", "110014", AvailabilityStatus.Available, now.AddDays(-60)),

            // Northwest Delhi Zone
            Profile(users[9], BloodGroup.OPositive, "Delhi", "110032", AvailabilityStatus.Available, now.AddDays(-15)),

            // Additional donors for testing various blood types and pincodes
            Profile(users[10], BloodGroup.ABPositive, "Delhi", "110016", AvailabilityStatus.Available, null),
            Profile(users[11], BloodGroup.ONegative, "Delhi", "110024", AvailabilityStatus.Available, now.AddDays(-70))
        };

        db.DonorProfiles.AddRange(profiles);

        await db.SaveChangesAsync(cancellationToken);

        // Updated sample request for Delhi with pincode
        var sampleRequest = new BloodRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = users[6].Id, // Using Noah (actually now using Noah for one of the new Delhi users)
            BloodGroup = BloodGroup.OPositive,
            HospitalName = "Max Healthcare Institute, Delhi",
            City = "Delhi",
            Pincode = "110016", // South Delhi, Sector 16
            UnitsNeeded = 2,
            Urgency = UrgencyLevel.Urgent,
            Notes = "Scheduled emergency surgery; O+ donors needed urgently. Hospital location: South Delhi (Pincode 110016)",
            Status = RequestStatus.Open,
            CreatedUtc = now,
            UpdatedUtc = now,
            ExpiresUtc = now.AddHours(72)
        };

        db.BloodRequests.Add(sampleRequest);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DonorProfile Profile(
        AppUser user, BloodGroup group, string city, string? pincode, AvailabilityStatus availability, DateTime? lastDonation)
    {
        var now = DateTime.UtcNow;
        return new DonorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BloodGroup = group,
            City = city,
            Pincode = pincode,
            Availability = availability,
            LastDonationUtc = lastDonation,
            ContactPreference = ContactPreference.TeamsChat,
            HasOptedIn = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }
}
