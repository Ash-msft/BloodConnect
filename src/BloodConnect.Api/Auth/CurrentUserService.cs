using System.Security.Claims;
using BloodConnect.Domain;
using BloodConnect.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BloodConnect.Api.Auth;

/// <summary>
/// Resolves the authenticated caller's <see cref="AppUser"/> record from the validated identity claim
/// on <see cref="HttpContext.User"/>. This is the single source of truth for "who am I" throughout the
/// API — controllers must never accept a user id from the request body/query string as authoritative.
/// </summary>
public interface ICurrentUserService
{
    Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly BloodConnectDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, BloodConnectDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public async Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var externalId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        return await _db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
    }
}
