using BloodConnect.Api.Auth;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using BloodConnect.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Api.Controllers;

/// <summary>
/// Identity endpoints: who the caller currently is, and (only meaningful under demo auth) the list
/// of selectable demo identities. Never exposes other users' donor profile data.
/// </summary>
[ApiController]
[Route("api")]
public class MeController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly BloodConnectDbContext _db;

    public MeController(ICurrentUserService currentUser, BloodConnectDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> GetMe(CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            throw new ForbiddenException("Unable to resolve the authenticated user.");
        }

        var hasDonorProfile = await _db.DonorProfiles.AnyAsync(p => p.UserId == user.Id, cancellationToken);

        return Ok(new CurrentUserDto(user.Id, user.DisplayName, user.Email, hasDonorProfile));
    }

    [HttpGet("demo-users")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<DemoUserOptionDto>), StatusCodes.Status200OK)]
    public ActionResult<List<DemoUserOptionDto>> GetDemoUsers()
    {
        var options = Infrastructure.Seed.DemoDataSeeder.DemoUsers
            .Select(u => new DemoUserOptionDto(u.ExternalId, u.DisplayName))
            .ToList();

        return Ok(options);
    }
}
