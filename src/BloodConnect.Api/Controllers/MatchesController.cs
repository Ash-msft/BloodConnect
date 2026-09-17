using BloodConnect.Api.Auth;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using BloodConnect.Infrastructure;
using BloodConnect.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Api.Controllers;

/// <summary>
/// The donor-facing side of the matching flow: requests matched to the caller, and the caller's
/// own Available/Not Available response. A donor only ever sees requests they were matched to —
/// never a global list of all open requests — and only their own response record.
/// </summary>
[ApiController]
[Authorize]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly BloodConnectDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationService _notifications;

    public MatchesController(
        BloodConnectDbContext db, ICurrentUserService currentUser, NotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<MatchedRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MatchedRequestDto>>> GetMyMatches(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);

        var matches = await _db.DonorResponses
            .Include(r => r.BloodRequest)
            .Where(r => r.DonorUserId == user.Id && r.BloodRequest != null)
            .OrderByDescending(r => r.NotifiedUtc)
            .Select(r => new MatchedRequestDto(
                r.BloodRequest!.Id,
                r.BloodRequest.BloodGroup,
                r.BloodRequest.HospitalName,
                r.BloodRequest.City,
                r.BloodRequest.UnitsNeeded,
                r.BloodRequest.Urgency,
                r.BloodRequest.Notes,
                r.Status,
                r.Id,
                r.NotifiedUtc))
            .ToListAsync(cancellationToken);

        return Ok(matches);
    }

    [HttpPost("{responseId:guid}/respond")]
    [ProducesResponseType(typeof(MatchedRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchedRequestDto>> Respond(
        Guid responseId, [FromBody] RespondToRequestDto body, CancellationToken cancellationToken)
    {
        if (body.Response is not (ResponseStatus.Available or ResponseStatus.NotAvailable))
        {
            throw new ValidationException("Response must be either Available or NotAvailable.");
        }

        var user = await RequireUserAsync(cancellationToken);

        var response = await _db.DonorResponses
            .Include(r => r.BloodRequest)
            .FirstOrDefaultAsync(r => r.Id == responseId, cancellationToken);

        if (response is null)
        {
            throw new NotFoundApiException("Matched request response not found.");
        }

        if (response.DonorUserId != user.Id)
        {
            throw new ForbiddenException("You may only respond to your own matched requests.");
        }

        response.Status = body.Response;
        response.RespondedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        if (body.Response == ResponseStatus.Available && response.BloodRequest is not null)
        {
            await _notifications.CreateAndDeliverAsync(
                response.BloodRequest.RequesterId,
                NotificationType.DonorAvailableResponse,
                response.BloodRequest.Id,
                "A donor is available",
                "A matched donor has responded that they are available. Open the request to see their contact details.",
                "{}",
                cancellationToken);
        }

        var dto = new MatchedRequestDto(
            response.BloodRequest!.Id,
            response.BloodRequest.BloodGroup,
            response.BloodRequest.HospitalName,
            response.BloodRequest.City,
            response.BloodRequest.UnitsNeeded,
            response.BloodRequest.Urgency,
            response.BloodRequest.Notes,
            response.Status,
            response.Id,
            response.NotifiedUtc);

        return Ok(dto);
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
