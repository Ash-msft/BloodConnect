using BloodConnect.Api.Auth;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using BloodConnect.Infrastructure;
using BloodConnect.Infrastructure.Locations;
using BloodConnect.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodConnect.Api.Controllers;

/// <summary>
/// Manages the caller's own private donor profile. There is no endpoint that lists other users'
/// donor profiles — donor identity is only ever revealed via the request/response flow, and only
/// once the donor affirmatively responds "Available".
/// </summary>
[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly BloodConnectDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly BloodConnectOptions _options;

    public ProfileController(
        BloodConnectDbContext db, ICurrentUserService currentUser, IOptions<BloodConnectOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _options = options.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DonorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonorProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);

        var profile = await _db.DonorProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        if (profile is null)
        {
            throw new NotFoundApiException("You have not created a donor profile yet.");
        }

        return Ok(ToDto(profile));
    }

    [HttpPut]
    [ProducesResponseType(typeof(DonorProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DonorProfileDto>> UpsertMyProfile(
        [FromBody] UpsertDonorProfileRequest request, CancellationToken cancellationToken)
    {
        Validate(request);

        var user = await RequireUserAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var profile = await _db.DonorProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        if (profile is null)
        {
            profile = new DonorProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedUtc = now
            };
            _db.DonorProfiles.Add(profile);
        }

        profile.BloodGroup = request.BloodGroup;
        profile.City = request.City.Trim();
        profile.Pincode = string.IsNullOrWhiteSpace(request.Pincode) ? null : request.Pincode.Trim();
        profile.Availability = request.Availability;
        profile.LastDonationUtc = request.LastDonationUtc;
        profile.ContactPreference = request.ContactPreference;
        profile.ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim();
        profile.HasOptedIn = request.HasOptedIn;
        profile.UpdatedUtc = now;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(profile));
    }

    private void Validate(UpsertDonorProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.City))
        {
            throw new ValidationException("City is required.");
        }

        if (request.City.Trim().Length > 120)
        {
            throw new ValidationException("City must be 120 characters or fewer.");
        }

        if (request.LastDonationUtc is { } lastDonation && lastDonation > DateTime.UtcNow)
        {
            throw new ValidationException("Last donation date cannot be in the future.");
        }

        if (request.ContactPreference == ContactPreference.Phone && string.IsNullOrWhiteSpace(request.ContactPhone))
        {
            throw new ValidationException("A contact phone number is required when phone is the preferred contact method.");
        }

        if (!string.IsNullOrWhiteSpace(request.ContactPhone) && request.ContactPhone.Trim().Length > 30)
        {
            throw new ValidationException("Contact phone must be 30 characters or fewer.");
        }

        ValidatePincode(request.City, request.Pincode);
    }

    /// <summary>
    /// Pincode is optional, but when supplied for Delhi it must be one we can place on the map —
    /// otherwise proximity matching would silently degrade.
    /// </summary>
    private static void ValidatePincode(string city, string? pincode)
    {
        if (string.IsNullOrWhiteSpace(pincode))
        {
            return;
        }

        var trimmed = pincode.Trim();

        if (trimmed.Length > 10)
        {
            throw new ValidationException("Pincode must be 10 characters or fewer.");
        }

        var isDelhi = string.Equals(city.Trim(), "Delhi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(city.Trim(), "New Delhi", StringComparison.OrdinalIgnoreCase);

        if (isDelhi && !DelhiPincodeService.IsValidDelhiPincode(trimmed))
        {
            throw new ValidationException(
                $"'{trimmed}' is not a recognised Delhi pincode. Provide a valid 6-digit Delhi pincode (for example 110016) or leave it blank.");
        }
    }

    private DonorProfileDto ToDto(DonorProfile profile)
    {
        var now = DateTime.UtcNow;
        var isEligible = BloodEligibilityCalculator.IsEligible(
            profile.LastDonationUtc, now, _options.MinimumDonationIntervalDays);
        var nextEligible = BloodEligibilityCalculator.GetNextEligibleUtc(
            profile.LastDonationUtc, _options.MinimumDonationIntervalDays);

        return new DonorProfileDto(
            profile.BloodGroup,
            profile.City,
            profile.Pincode,
            profile.Availability,
            profile.LastDonationUtc,
            profile.ContactPreference,
            profile.ContactPhone,
            profile.HasOptedIn,
            isEligible,
            nextEligible,
            DelhiPincodeService.GetZoneName(profile.Pincode));
    }

    private async Task<AppUser> RequireUserAsync(CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            throw new ForbiddenException("Unable to resolve the authenticated user.");
        }

        return user;
    }
}
