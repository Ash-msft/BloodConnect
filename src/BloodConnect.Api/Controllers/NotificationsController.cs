using BloodConnect.Api.Auth;
using BloodConnect.Api.Dtos;
using BloodConnect.Domain;
using BloodConnect.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodConnect.Api.Controllers;

/// <summary>
/// The caller's own notification inbox — a durable, observable view of every notification recorded
/// in the outbox for them, regardless of delivery channel.
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationService _notifications;

    public NotificationsController(ICurrentUserService currentUser, NotificationService notifications)
    {
        _currentUser = currentUser;
        _notifications = notifications;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<NotificationDto>>> GetInbox(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var items = await _notifications.GetInboxAsync(user.Id, cancellationToken);

        var dtos = items.Select(n => new NotificationDto(
            n.Id, n.Type, n.BloodRequestId, n.Title, n.Body, n.AdaptiveCardJson,
            n.Status, n.Channel, n.DeliveryDetail, n.CreatedUtc, n.ReadByRecipient)).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        var found = await _notifications.MarkReadAsync(user.Id, id, cancellationToken);

        if (!found)
        {
            throw new NotFoundApiException("Notification not found.");
        }

        return NoContent();
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
