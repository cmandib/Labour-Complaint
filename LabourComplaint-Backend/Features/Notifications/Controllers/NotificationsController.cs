// Features/Notifications/Controllers/NotificationsController.cs
using System.Security.Claims;
using LabourComplaint_Backend.Features.Notifications.Dtos;
using LabourComplaint_Backend.Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabourComplaint_Backend.Features.Notifications.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(NotificationResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationResponseDto>> GetNotifications(
        [FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var result = await _service.GetNotificationsAsync(userId, unreadOnly, page, limit, ct);
        return result.Match<ActionResult<NotificationResponseDto>>(
            onSuccess: res => Ok(res),
            onFailure: err => BadRequest(new { error = err }));
    }

    [HttpPost("{id}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var result = await _service.MarkAsReadAsync(id, userId, ct);
        return result.Match<ActionResult>(
            onSuccess: _ => Ok(),
            onFailure: err => NotFound(new { error = err }));
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _service.MarkAllAsReadAsync(userId, ct);
        return Ok();
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
    }
}