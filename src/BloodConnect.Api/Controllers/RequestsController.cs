using BloodConnect.Api.Auth;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using BloodConnect.Infrastructure;
using BloodConnect.Infrastructure.Locations;
using BloodConnect.Infrastructure.Matching;
using BloodConnect.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BloodConnect.Api.Controllers;

/// <summary>
/// Blood request lifecycle: creation (which triggers donor matching/notification), listing the
/// caller's own requests, and viewing responses. Donor identity/contact details for a request's
/// responses are only ever included for donors who have responded "Available" — enforced here,
/// not left to the client.
/// </summary>
[ApiController]
[Authorize]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly BloodConnectDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly DonorMatchingService _matchingService;
    private readonly BloodConnectOptions _options;

    public RequestsController(
        BloodConnectDbContext db,
        ICurrentUserService currentUser,
        DonorMatchingService matchingService,
        IOptions<BloodConnectOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _matchingService = matchingService;
        _options = options.Value;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BloodRequestDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<BloodRequestDto>> Create(
        [FromBody] CreateBloodRequestDto request, CancellationToken cancellationToken)
    {
        Validate(request);

        var user = await RequireUserAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var entity = new BloodRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = user.Id,
            BloodGroup = request.BloodGroup,
            HospitalName = request.HospitalName.Trim(),
            City = request.City.Trim(),
            Pincode = string.IsNullOrWhiteSpace(request.Pincode) ? null : request.Pincode.Trim(),
            UnitsNeeded = request.UnitsNeeded,
            Urgency = request.Urgency,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = RequestStatus.Open,
            CreatedUtc = now,
            UpdatedUtc = now,
            ExpiresUtc = now.AddHours(_options.RequestExpiryHours)
        };

        _db.BloodRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _matchingService.MatchAndNotifyAsync(entity, cancellationToken);

        var dto = await BuildRequestDtoAsync(entity, user.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BloodRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BloodRequestDto>>> GetMyRequests(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        await ExpireStaleRequestsAsync(cancellationToken);

        var requests = await _db.BloodRequests
            .Where(r => r.RequesterId == user.Id)
            .OrderByDescending(r => r.CreatedUtc)
            .ToListAsync(cancellationToken);

        var dtos = new List<BloodRequestDto>();
        foreach (var request in requests)
        {
            dtos.Add(await BuildRequestDtoAsync(request, user.Id, cancellationToken));
        }

        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BloodRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloodRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var request = await FindOwnedRequestAsync(id, user.Id, cancellationToken);
        return Ok(await BuildRequestDtoAsync(request, user.Id, cancellationToken));
    }

    [HttpGet("{id:guid}/responses")]
    [ProducesResponseType(typeof(List<DonorResponseForRequesterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DonorResponseForRequesterDto>>> GetResponses(
        Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var request = await FindOwnedRequestAsync(id, user.Id, cancellationToken);

        var responses = await _db.DonorResponses
            .Include(r => r.DonorUser)
            .Include(r => r.DonorUser!.DonorProfile)
            .Where(r => r.BloodRequestId == request.Id)
            .OrderBy(r => r.ProximityRank)
            .ThenByDescending(r => r.Status == ResponseStatus.Available)
            .ToListAsync(cancellationToken);

        var dtos = responses.Select(r =>
        {
            var revealIdentity = r.Status == ResponseStatus.Available;
            return new DonorResponseForRequesterDto(
                r.Id,
                r.Status,
                r.ProximityRank,
                r.NotifiedUtc,
                r.RespondedUtc,
                revealIdentity ? r.DonorUser?.DisplayName : null,
                revealIdentity ? r.DonorUser?.Email : null,
                revealIdentity ? r.DonorUser?.DonorProfile?.ContactPhone : null,
                revealIdentity ? r.DonorUser?.DonorProfile?.ContactPreference : null);
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(BloodRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BloodRequestDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var request = await FindOwnedRequestAsync(id, user.Id, cancellationToken);

        if (request.Status is RequestStatus.Fulfilled or RequestStatus.Cancelled)
        {
            throw new ValidationException($"Request is already {request.Status} and cannot be cancelled.");
        }

        request.Status = RequestStatus.Cancelled;
        request.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(await BuildRequestDtoAsync(request, user.Id, cancellationToken));
    }

    [HttpPost("{id:guid}/fulfill")]
    [ProducesResponseType(typeof(BloodRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BloodRequestDto>> Fulfill(Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var request = await FindOwnedRequestAsync(id, user.Id, cancellationToken);

        if (request.Status is RequestStatus.Fulfilled or RequestStatus.Cancelled)
        {
            throw new ValidationException($"Request is already {request.Status}.");
        }

        request.Status = RequestStatus.Fulfilled;
        request.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(await BuildRequestDtoAsync(request, user.Id, cancellationToken));
    }

    private async Task ExpireStaleRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var stale = await _db.BloodRequests
            .Where(r => r.Status == RequestStatus.Open && r.ExpiresUtc <= now)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        foreach (var request in stale)
        {
            request.Status = RequestStatus.Expired;
            request.UpdatedUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<BloodRequest> FindOwnedRequestAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var request = await _db.BloodRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request is null)
        {
            throw new NotFoundApiException("Blood request not found.");
        }

        if (request.RequesterId != userId)
        {
            throw new ForbiddenException("You may only view or manage requests that you created.");
        }

        return request;
    }

    private async Task<BloodRequestDto> BuildRequestDtoAsync(
        BloodRequest request, Guid callerId, CancellationToken cancellationToken)
    {
        var matchedCount = await _db.DonorResponses.CountAsync(r => r.BloodRequestId == request.Id, cancellationToken);
        var availableCount = await _db.DonorResponses.CountAsync(
            r => r.BloodRequestId == request.Id && r.Status == ResponseStatus.Available, cancellationToken);

        return new BloodRequestDto(
            request.Id,
            request.BloodGroup,
            request.HospitalName,
            request.City,
            request.Pincode,
            request.UnitsNeeded,
            request.Urgency,
            request.Notes,
            request.Status,
            request.CreatedUtc,
            request.ExpiresUtc,
            matchedCount,
            availableCount,
            request.RequesterId == callerId,
            DelhiPincodeService.GetZoneName(request.Pincode),
            DonorMatchingService.GetSearchRadiusKm(request.Urgency));
    }

    private void Validate(CreateBloodRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.HospitalName))
        {
            throw new ValidationException("Hospital name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            throw new ValidationException("City is required.");
        }

        if (request.UnitsNeeded is < 1 or > 50)
        {
            throw new ValidationException("Units needed must be between 1 and 50.");
        }

        if (request.Notes is { Length: > 1000 })
        {
            throw new ValidationException("Notes must be 1000 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(request.Pincode))
        {
            var pincode = request.Pincode.Trim();

            if (pincode.Length > 10)
            {
                throw new ValidationException("Pincode must be 10 characters or fewer.");
            }

            var city = request.City.Trim();
            var isDelhi = string.Equals(city, "Delhi", StringComparison.OrdinalIgnoreCase)
                || string.Equals(city, "New Delhi", StringComparison.OrdinalIgnoreCase);

            if (isDelhi && !DelhiPincodeService.IsValidDelhiPincode(pincode))
            {
                throw new ValidationException(
                    $"'{pincode}' is not a recognised Delhi pincode. Provide a valid 6-digit Delhi pincode (for example 110016) or leave it blank.");
            }
        }
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
