// Features/Chat/Services/ChatService.cs
using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Features.Chat.Dtos;
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using LabourComplaint_Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace LabourComplaint_Backend.Features.Chat.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _db;

    public ChatService(ApplicationDbContext db) => _db = db;

    public async Task<Result<IEnumerable<ChatMessageDto>>> GetMessagesAsync(
    int chatRoomId,
    int requestingUserId,
    int page = 1,
    int limit = 50,
    CancellationToken ct = default)
    {
        // 1. Fetch chat room to determine participants and verify access
        var chatRoom = await _db.ChatRooms
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId && !cr.IsDeleted, ct);

        if (chatRoom == null ||
            (chatRoom.CitizenId != requestingUserId && chatRoom.InspectorId != requestingUserId))
        {
            return Result<IEnumerable<ChatMessageDto>>.Failure("Access denied to this chat room");
        }

        // 2. Determine the OTHER participant's ID
        var otherUserId = requestingUserId == chatRoom.CitizenId
            ? chatRoom.InspectorId
            : chatRoom.CitizenId;

        // 3. Auto-mark unread messages FROM the other participant AS READ
        // This happens regardless of pagination - opening the chat = reading the conversation

        // EF Core 7+ approach (efficient bulk update without tracking):
        await _db.Messages
            .Where(m => m.ChatRoomId == chatRoomId
                     && m.SenderId == otherUserId
                     && !m.IsRead
                     && !m.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAt, DateTime.UtcNow), ct);

        // EF Core 6 or earlier alternative (load into memory, then save):
        /*
        var unread = await _db.Messages
            .Where(m => m.ChatRoomId == chatRoomId 
                     && m.SenderId == otherUserId 
                     && !m.IsRead 
                     && !m.IsDeleted)
            .ToListAsync(ct);

        if (unread.Any())
        {
            foreach (var msg in unread)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
        }
        */

        // 4. Fetch messages for display (pagination + projection to DTO)
        var messages = await _db.Messages
            .AsNoTracking() // Read-only query: no change tracking overhead
            .Where(m => m.ChatRoomId == chatRoomId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender.Username, // Ensure navigation property is configured
                Content = m.Content,
                Direction = m.Direction.ToString(),
                FileUrl = m.FileUrl,
                IsRead = m.IsRead, // Will reflect the updated read status
                SentAt = m.CreatedAt
            })
            .ToListAsync(ct);

        // 5. Return chronologically ordered (oldest to newest) for UI display
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
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId && !cr.IsDeleted, ct);

        if (chatRoom is null) return Result<ChatMessageDto>.Failure("Chat room not found");
        if (chatRoom.CitizenId != senderId && chatRoom.InspectorId != senderId)
            return Result<ChatMessageDto>.Failure("You are not a participant in this chat");

        // 🔑 Fixed: Compare enum directly (not string)
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

            // 🔑 Update preview & timestamp (properties now exist)
            chatRoom.LastMessagePreview = message.Content.Length > 100
                ? message.Content[..100] + "..."
                : message.Content;
            chatRoom.LastMessageAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var senderName = senderId == chatRoom.CitizenId
            ? chatRoom.Citizen.Username
            : chatRoom.Inspector.Username;

        return Result<ChatMessageDto>.Success(new ChatMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = senderName,
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
        // Verify access to chat room
        var chatRoom = await _db.ChatRooms
            .FirstOrDefaultAsync(cr => cr.Id == chatRoomId && !cr.IsDeleted, ct);

        if (chatRoom is null)
            return Result<int>.Failure("Chat room not found");

        if (chatRoom.CitizenId != userId && chatRoom.InspectorId != userId)
            return Result<int>.Failure("Access denied");

        // Mark unread messages FROM THE OTHER PARTICIPANT as read
        // (You don't mark your own messages as read)
        var otherUserId = userId == chatRoom.CitizenId
            ? chatRoom.InspectorId
            : chatRoom.CitizenId;

        var unreadMessages = await _db.Messages
            .Where(m => m.ChatRoomId == chatRoomId
                     && m.SenderId == otherUserId  // Messages from the other person
                     && !m.IsRead
                     && !m.IsDeleted)
            .ToListAsync(ct);

        if (!unreadMessages.Any())
            return Result<int>.Success(0); // Nothing to update

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(unreadMessages.Count);
    }
}