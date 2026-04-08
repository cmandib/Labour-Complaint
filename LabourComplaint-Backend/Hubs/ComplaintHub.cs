// Hubs/ComplaintHub.cs
using System.Security.Claims;
using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LabourComplaint_Backend.Hubs;

[Authorize]
public class ComplaintHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ComplaintHub> _logger;

    public ComplaintHub(ApplicationDbContext db, ILogger<ComplaintHub> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task JoinComplaint(string referenceNumber)
    {
        var userId = Context.User?.FindFirst("sub")?.Value
                  ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new HubException("Unauthorized: User ID not found in token");

        // Verify user has access to this complaint
        var complaint = await _db.Complaints
            .FirstOrDefaultAsync(c => c.ReferenceNumber == referenceNumber && !c.IsDeleted);

        if (complaint is null)
            throw new HubException("Complaint not found");

        var hasAccess = complaint.ReporterId.ToString() == userId
                     || complaint.AssignedInspectorId?.ToString() == userId
                     || Context.User?.IsInRole("Admin") == true;

        if (!hasAccess)
            throw new HubException("Access denied to this complaint");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"complaint-{referenceNumber}");
        _logger.LogInformation("User {UserId} joined complaint {Ref}", userId, referenceNumber);
    }

    // 🔑 UPDATED: Save to DB + broadcast
    public async Task SendChatMessage(string referenceNumber, string senderName, string content)
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
                       ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var senderId))
            throw new HubException("Unauthorized: Invalid user ID");

        // 1. Find the complaint + chat room
        var complaint = await _db.Complaints
            .Include(c => c.ChatRoom)
            .FirstOrDefaultAsync(c => c.ReferenceNumber == referenceNumber && !c.IsDeleted);

        if (complaint?.ChatRoom is null)
            throw new HubException("Chat room not found for this complaint");

        var chatRoom = complaint.ChatRoom;

        // 2. Verify sender is a participant
        if (chatRoom.CitizenId != senderId && chatRoom.InspectorId != senderId)
            throw new HubException("You are not authorized to send messages in this chat");

        if (chatRoom.Status != ChatRoomStatus.Open)
            throw new HubException("Cannot send messages to a closed chat");

        // 3. Determine direction
        var direction = senderId == chatRoom.CitizenId
            ? MessageDirection.CitizenToInspector
            : MessageDirection.InspectorToCitizen;

        // 4. Create and save message entity
        var message = new Message
        {
            ChatRoomId = chatRoom.Id,
            SenderId = senderId,
            Content = content.Trim(),
            Direction = direction,
            IsRead = false,
            ShouldNotify = true,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
            // Add FileUrl, FileName, etc. if your client sends them
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        // 5. Update chat room preview
        chatRoom.LastMessagePreview = content.Length > 100 ? content[..100] + "..." : content;
        chatRoom.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 6. Broadcast to group (realtime)
        await Clients.Group($"complaint-{referenceNumber}")
            .SendAsync("ReceiveMessage", senderName, content, message.CreatedAt);

        _logger.LogInformation("Message saved + broadcast: {Ref} by {Sender}", referenceNumber, senderName);
    }

    public async Task LeaveComplaint(string referenceNumber)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"complaint-{referenceNumber}");
    }
}