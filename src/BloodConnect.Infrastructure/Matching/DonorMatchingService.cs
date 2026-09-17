using BloodConnect.Domain;
using BloodConnect.Infrastructure.Locations;
using BloodConnect.Infrastructure.Notifications;
using BloodConnect.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodConnect.Infrastructure.Matching;

/// <summary>
/// Finds compatible, available, and eligible donors for a blood request, creates pending
/// <see cref="DonorResponse"/> records for them, and notifies each matched donor individually.
/// 
/// Matching prioritization (in order):
/// 1. Blood type compatibility (ABO/Rh matrix)
/// 2. Availability status
/// 3. Medical eligibility (donation interval)
/// 4. Location proximity (pincode-based for Delhi)
/// 5. Urgency level (high/critical = notify closest donors)
/// 
/// Only the matched donor and the requester (indirectly, once the donor responds) ever see
/// donor-specific data — no bulk donor list is exposed to requesters.
/// </summary>
public class DonorMatchingService
{
    private readonly BloodConnectDbContext _db;
    private readonly NotificationService _notifications;
    private readonly BloodConnectOptions _options;
    private readonly TeamsNotificationOptions _teamsOptions;

    public DonorMatchingService(
        BloodConnectDbContext db,
        NotificationService notifications,
        IOptions<BloodConnectOptions> options,
        IOptions<TeamsNotificationOptions> teamsOptions)
    {
        _db = db;
        _notifications = notifications;
        _options = options.Value;
        _teamsOptions = teamsOptions.Value;
    }

    /// <summary>
    /// Matches and notifies eligible donors for the given request. Safe to call multiple times;
    /// donors who already have a response record for this request are not re-notified.
    /// 
    /// For Delhi requests: uses pincode-based proximity matching.
    /// For other cities: uses city-name matching (fallback, will be extended later).
    /// 
    /// High/critical urgency: notifies closest donors who can reach fastest.
    /// Low/medium urgency: broader geographic radius.
    /// </summary>
    public async Task<int> MatchAndNotifyAsync(BloodRequest request, CancellationToken cancellationToken)
    {
        var compatibleGroups = BloodCompatibility.GetCompatibleDonorGroups(request.BloodGroup);
        var now = DateTime.UtcNow;

        var alreadyNotified = await _db.DonorResponses
            .Where(r => r.BloodRequestId == request.Id)
            .Select(r => r.DonorUserId)
            .ToListAsync(cancellationToken);

        var candidates = await _db.DonorProfiles
            .Include(p => p.User)
            .Where(p => p.HasOptedIn
                        && p.Availability == AvailabilityStatus.Available
                        && compatibleGroups.Contains(p.BloodGroup)
                        && p.UserId != request.RequesterId
                        && !alreadyNotified.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        var eligibleDonors = candidates
            .Where(p => BloodEligibilityCalculator.IsEligible(p.LastDonationUtc, now, _options.MinimumDonationIntervalDays))
            .ToList();

        // Sort by proximity and urgency
        var prioritizedDonors = PrioritizeDonorsByLocation(eligibleDonors, request)
            .Take(_options.MaxDonorsNotifiedPerRequest)
            .ToList();

        var cardJson = AdaptiveCardBuilder.BuildNewRequestCard(request, _teamsOptions.AppBaseUrl);

        foreach (var donor in prioritizedDonors)
        {
            var proximityRank = CalculateProximityRank(donor, request);

            var response = new DonorResponse
            {
                Id = Guid.NewGuid(),
                BloodRequestId = request.Id,
                DonorUserId = donor.UserId,
                Status = ResponseStatus.Pending,
                ProximityRank = proximityRank,
                NotifiedUtc = now
            };

            _db.DonorResponses.Add(response);
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var donor in prioritizedDonors)
        {
            await _notifications.CreateAndDeliverAsync(
                donor.UserId,
                NotificationType.NewMatchingRequest,
                request.Id,
                "New blood request matches your profile",
                $"A request for {request.BloodGroup} at {request.HospitalName}, {request.City} needs {request.UnitsNeeded} unit(s). Urgency: {request.Urgency}",
                cardJson,
                cancellationToken);
        }

        return prioritizedDonors.Count;
    }

    /// <summary>
    /// Prioritizes donors by location and urgency.
    /// For Delhi: uses pincode-based distance.
    /// For other cities: uses city-name matching (to be extended).
    /// High/critical urgency: prioritizes closest donors.
    /// </summary>
    private IEnumerable<DonorProfile> PrioritizeDonorsByLocation(
        List<DonorProfile> eligibleDonors,
        BloodRequest request)
    {
        // For Delhi: use pincode-based prioritization
        if (string.Equals(request.City, "Delhi", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.City, "New Delhi", StringComparison.OrdinalIgnoreCase))
        {
            return PrioritizeDonorsForDelhi(eligibleDonors, request);
        }

        // Fallback for other cities: use city-name matching
        return PrioritizeDonorsByCity(eligibleDonors, request);
    }

    /// <summary>
    /// Delhi-specific matching: prioritizes by pincode distance and urgency.
    /// </summary>
    private IEnumerable<DonorProfile> PrioritizeDonorsForDelhi(
        List<DonorProfile> eligibleDonors,
        BloodRequest request)
    {
        var requestPincode = request.Pincode;

        // Without a resolvable request pincode we cannot compute any distance, so fall back to
        // city matching rather than silently notifying nobody.
        if (!DelhiPincodeService.IsValidDelhiPincode(requestPincode))
        {
            return PrioritizeDonorsByCity(eligibleDonors, request);
        }

        var radiusKm = GetSearchRadiusKm(request.Urgency);

        var donorsWithDistance = eligibleDonors
            .Select(donor => new
            {
                Donor = donor,
                Distance = DelhiPincodeService.CalculateDistanceKm(donor.Pincode, requestPincode)
            })
            .ToList();

        // Tier 1: donors with a known pincode inside the urgency radius, closest first.
        var withinRadius = donorsWithDistance
            .Where(d => d.Distance is { } km && km <= radiusKm)
            .OrderBy(d => d.Distance!.Value)
            .ThenBy(d => d.Donor.LastDonationUtc ?? DateTime.MinValue)
            .Select(d => d.Donor);

        // Tier 2: donors whose pincode is missing or outside the Delhi dataset. Their distance is
        // unknown (not "far"), so they are kept as a lower-priority fallback instead of dropped.
        var unknownDistance = donorsWithDistance
            .Where(d => d.Distance is null)
            .OrderBy(d => d.Donor.LastDonationUtc ?? DateTime.MinValue)
            .Select(d => d.Donor);

        // Tier 3: donors with a known pincode beyond the radius, closest first. Included last so
        // that an urgent request still reaches someone when the immediate area has no donors.
        var outsideRadius = donorsWithDistance
            .Where(d => d.Distance is { } km && km > radiusKm)
            .OrderBy(d => d.Distance!.Value)
            .ThenBy(d => d.Donor.LastDonationUtc ?? DateTime.MinValue)
            .Select(d => d.Donor);

        return withinRadius.Concat(unknownDistance).Concat(outsideRadius);
    }

    /// <summary>
    /// Search radius in kilometres for a given urgency level. Tighter radii for more urgent
    /// requests so that the donors notified are the ones who can physically reach the hospital
    /// fastest.
    /// </summary>
    public static double GetSearchRadiusKm(UrgencyLevel urgency) => urgency switch
    {
        UrgencyLevel.Critical => 3.0,    // Critical: within 3 km (can reach in <10 min)
        UrgencyLevel.Urgent => 5.0,      // Urgent: within 5 km (can reach in ~15 min)
        UrgencyLevel.Routine => 10.0,    // Routine: within 10 km (can reach in ~30 min)
        _ => 10.0
    };

    /// <summary>
    /// Fallback for non-Delhi cities: prioritizes by city match and donation recency.
    /// </summary>
    private IEnumerable<DonorProfile> PrioritizeDonorsByCity(
        List<DonorProfile> eligibleDonors,
        BloodRequest request)
    {
        return eligibleDonors
            .OrderBy(p => string.Equals(p.City, request.City, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(p => p.LastDonationUtc ?? DateTime.MinValue);
    }

    /// <summary>
    /// Calculates proximity rank for a donor relative to request location.
    /// Lower ranks = closer = higher priority.
    /// </summary>
    private int CalculateProximityRank(DonorProfile donor, BloodRequest request)
    {
        // For Delhi: use pincode distance
        if (string.Equals(request.City, "Delhi", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.City, "New Delhi", StringComparison.OrdinalIgnoreCase))
        {
            var distanceKm = DelhiPincodeService.CalculateDistanceKm(donor.Pincode, request.Pincode);
            if (distanceKm is { } km)
            {
                if (km <= 3) return 0;  // Very close (critical response zone)
                if (km <= 5) return 1;  // Close (high urgency zone)
                if (km <= 10) return 2; // Moderate distance
                return 3; // Far
            }
        }

        // Fallback: city match
        return string.Equals(donor.City, request.City, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }
}

