using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Features.Chat.Dtos;
using LabourComplaint_Backend.Features.Notifications.Dtos; // Ensure this namespace contains NotificationCreateRequest
using LabourComplaint_Backend.Features.Notifications.Services;
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using LabourComplaint_Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace LabourComplaint_Backend.Features.Chat.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public ChatService(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<Result<IEnumerable<ChatMessageDto>>> GetMessagesAsync(
        int chatRoomId,
        int requestingUserId,
        int page = 1,
        int limit = 50,
        CancellationToken ct = default)
    {
        var chatRoom = await _db.ChatRooms
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId && !cr.IsDeleted, ct);

        if (chatRoom == null ||
            (chatRoom.CitizenId != requestingUserId && chatRoom.InspectorId != requestingUserId))
        {
            return Result<IEnumerable<ChatMessageDto>>.Failure("Access denied to this chat room");
        }

        var otherUserId = requestingUserId == chatRoom.CitizenId
            ? chatRoom.InspectorId
            : chatRoom.CitizenId;

        // Mark messages as read efficiently
        await _db.Messages
            .Where(m => m.ChatRoomId == chatRoomId
                     && m.SenderId == otherUserId
                     && !m.IsRead
                     && !m.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAt, DateTime.UtcNow), ct);

        var messages = await _db.Messages
            .AsNoTracking()
            .Where(m => m.ChatRoomId == chatRoomId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender.Username,
                Content = m.Content,
                Direction = m.Direction.ToString(),
                FileUrl = m.FileUrl,
                IsRead = m.IsRead,
                SentAt = m.CreatedAt
            })
            .ToListAsync(ct);

        return Result<IEnumerable<ChatMessageDto>>.Success(
            messages.OrderBy(m => m.SentAt)
        );
    }

    public async Task<Result<ChatMessageDto>> SendMessageAsync(
        int chatRoomId, int senderId, SendMessageDto dto, CancellationToken ct)
    {
        var chatRoom = await _db.ChatRooms
            .Include(cr => cr.Citizen)
            .Include(cr => cr.Inspector)
            .Include(cr => cr.Complaint) // Include Complaint to access ReferenceNumber
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId && !cr.IsDeleted, ct);

        if (chatRoom is null) return Result<ChatMessageDto>.Failure("Chat room not found");
        if (chatRoom.CitizenId != senderId && chatRoom.InspectorId != senderId)
            return Result<ChatMessageDto>.Failure("You are not a participant in this chat");

        if (chatRoom.Status != ChatRoomStatus.Open)
            return Result<ChatMessageDto>.Failure("Cannot send messages to a closed/archived chat");

        var direction = senderId == chatRoom.CitizenId
            ? MessageDirection.CitizenToInspector
            : MessageDirection.InspectorToCitizen;

        var message = new Message
        {
            ChatRoomId = chatRoomId,
            SenderId = senderId,
            Content = dto.Content.Trim(),
            Direction = direction,
            FileUrl = dto.FileUrl,
            FileName = dto.FileName,
            FileSizeBytes = dto.FileSizeBytes,
            FileMimeType = dto.FileMimeType,
            IsRead = false,
            ShouldNotify = true,
            CreatedAt = DateTime.UtcNow
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Messages.Add(message);
            await _db.SaveChangesAsync(ct);

            chatRoom.LastMessagePreview = message.Content.Length > 100
                ? message.Content[..100] + "..."
                : message.Content;
            chatRoom.LastMessageAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Prepare Notification Data
            var recipientId = senderId == chatRoom.CitizenId ? chatRoom.InspectorId : chatRoom.CitizenId;
            var senderName = senderId == chatRoom.CitizenId ? chatRoom.Citizen.Username : chatRoom.Inspector.Username;
            var preview = dto.Content.Length > 100 ? dto.Content[..100] + "..." : dto.Content;

            // Create Notification using the new structure
            await _notificationService.CreateAsync(new NotificationCreateRequest
            {
                RecipientId = recipientId,
                Type = NotificationType.NewMessage,
                Title = $"New message from {senderName}",
                Body = preview,

                Channel = NotificationChannel.InApp,
                Priority = PriorityLevel.Normal,

                // Context
                ComplaintId = chatRoom.ComplaintId,
                ChatRoomId = chatRoom.Id,
                MessageId = message.Id, // The message we just created
                TriggeredByUserId = senderId,

                // Action
                ActionUrl = $"/chat/{chatRoom.Id}",

                // Payload
                Payload = new
                {
                    MessageId = message.Id,
                    ChatRoomId = chatRoom.Id,
                    ComplaintReference = chatRoom.Complaint?.ReferenceNumber, // Safe navigation in case Complaint wasn't loaded fully or is null
                    SenderId = senderId,
                    HasAttachment = !string.IsNullOrEmpty(dto.FileUrl)
                },

                IsSystemGenerated = false, // User-triggered
                CorrelationId = Guid.NewGuid().ToString("N")
            }, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var finalSenderName = senderId == chatRoom.CitizenId
            ? chatRoom.Citizen.Username
            : chatRoom.Inspector.Username;

        return Result<ChatMessageDto>.Success(new ChatMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = finalSenderName,
            Content = message.Content,
            Direction = message.Direction.ToString(),
            FileUrl = message.FileUrl,
            IsRead = message.IsRead,
            SentAt = message.CreatedAt
        });
    }

    public async Task<Result<int>> MarkMessagesReadAsync(
        int chatRoomId, int userId, CancellationToken ct)
    {
        var chatRoom = await _db.ChatRooms
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId && !cr.IsDeleted, ct);

        if (chatRoom is null)
            return Result<int>.Failure("Chat room not found");

        if (chatRoom.CitizenId != userId && chatRoom.InspectorId != userId)
            return Result<int>.Failure("Access denied");

        var otherUserId = userId == chatRoom.CitizenId
            ? chatRoom.InspectorId
            : chatRoom.CitizenId;

        // Use ExecuteUpdate for better performance than fetching entities into memory
        var affectedRows = await _db.Messages
            .Where(m => m.ChatRoomId == chatRoomId
                     && m.SenderId == otherUserId
                     && !m.IsRead
                     && !m.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAt, DateTime.UtcNow), ct);

        return Result<int>.Success((int)affectedRows);
    }
}