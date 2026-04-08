// Features/Chat/Controllers/ChatController.cs
using System.Security.Claims;
using LabourComplaint_Backend.Features.Chat.Dtos;
using LabourComplaint_Backend.Features.Chat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabourComplaint_Backend.Features.Chat.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Tags("Chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _service;

    public ChatController(IChatService service) => _service = service;

    [HttpGet("rooms/{chatRoomId}/messages")]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetMessages(
        int chatRoomId, [FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var result = await _service.GetMessagesAsync(chatRoomId, userId, page, limit, ct);
        return result.Match<ActionResult<IEnumerable<ChatMessageDto>>>(
            onSuccess: messages => Ok(messages),
            onFailure: error => Forbid(error));
    }

    [HttpPost("rooms/{chatRoomId}/messages")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(
        int chatRoomId, [FromBody] SendMessageDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var result = await _service.SendMessageAsync(chatRoomId, userId, dto, ct);
        return result.Match<ActionResult<ChatMessageDto>>(
            onSuccess: msg => CreatedAtAction(nameof(GetMessages), new { chatRoomId }, msg),
            onFailure: error => BadRequest(new { error }));
    }
    /// <summary>
    /// Mark all unread messages in a chat room as read (for the current user)
    /// </summary>
    [HttpPost("rooms/{chatRoomId}/read")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> MarkMessagesRead(
        int chatRoomId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var result = await _service.MarkMessagesReadAsync(chatRoomId, userId, ct);

        return result.Match<ActionResult>(
            onSuccess: count => Ok(new { markedRead = count }),
            onFailure: error => Forbid(error));
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
    }
}