using BloodConnect.Api.Auth;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using BloodConnect.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Api.Controllers;

/// <summary>
/// Records and lists the caller's own donation history. Recording a donation updates the donor
/// profile's LastDonationUtc so future eligibility/matching immediately reflects it.
/// </summary>
[ApiController]
[Authorize]
[Route("api/donations")]
public class DonationsController : ControllerBase
{
    private readonly BloodConnectDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DonationsController(BloodConnectDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DonationHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DonationHistoryDto>>> GetMyHistory(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);

        var profile = await _db.DonorProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (profile is null)
        {
            return Ok(new List<DonationHistoryDto>());
        }

        var history = await _db.DonationHistoryEntries
            .Where(h => h.DonorProfileId == profile.Id)
            .OrderByDescending(h => h.DonationDateUtc)
            .Select(h => new DonationHistoryDto(h.Id, h.DonationDateUtc, h.Notes, h.FulfilledRequestId))
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DonationHistoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DonationHistoryDto>> RecordDonation(
        [FromBody] RecordDonationRequest request, CancellationToken cancellationToken)
    {
        if (request.DonationDateUtc > DateTime.UtcNow)
        {
            throw new ValidationException("Donation date cannot be in the future.");
        }

        if (request.Notes is { Length: > 1000 })
        {
            throw new ValidationException("Notes must be 1000 characters or fewer.");
        }

        var user = await RequireUserAsync(cancellationToken);

        var profile = await _db.DonorProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
        if (profile is null)
        {
            throw new ValidationException("Create a donor profile before recording a donation.");
        }

        if (request.FulfilledRequestId is { } requestId)
        {
            var requestExists = await _db.BloodRequests.AnyAsync(r => r.Id == requestId, cancellationToken);
            if (!requestExists)
            {
                throw new ValidationException("The referenced blood request does not exist.");
            }
        }

        var entry = new DonationHistoryEntry
        {
            Id = Guid.NewGuid(),
            DonorProfileId = profile.Id,
            DonationDateUtc = request.DonationDateUtc,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            FulfilledRequestId = request.FulfilledRequestId,
            RecordedUtc = DateTime.UtcNow
        };

        _db.DonationHistoryEntries.Add(entry);

        if (profile.LastDonationUtc is null || request.DonationDateUtc > profile.LastDonationUtc)
        {
            profile.LastDonationUtc = request.DonationDateUtc;
            profile.UpdatedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetMyHistory),
            new DonationHistoryDto(entry.Id, entry.DonationDateUtc, entry.Notes, entry.FulfilledRequestId));
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
