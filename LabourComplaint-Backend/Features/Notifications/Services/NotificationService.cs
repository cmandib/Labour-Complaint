using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Features.Notifications.Dtos;
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using LabourComplaint_Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace LabourComplaint_Backend.Features.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task<Result<NotificationResponseDto>> GetNotificationsAsync(
        int userId, bool unreadOnly = false, int page = 1, int limit = 20, CancellationToken ct = default)
    {
        var query = _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientId == userId && !n.IsDeleted);

        if (unreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Body = n.Body,
                ActionUrl = n.ActionUrl,
                IsRead = n.ReadAt != null,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);

        var unreadCount = unreadOnly ? totalCount :
            await _db.Notifications.CountAsync(n => n.RecipientId == userId && !n.IsDeleted && n.ReadAt == null, ct);

        return Result<NotificationResponseDto>.Success(new NotificationResponseDto
        {
            Items = items,
            UnreadCount = unreadCount
        });
    }

    public async Task<Result<bool>> MarkAsReadAsync(int notificationId, int userId, CancellationToken ct)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId && !n.IsDeleted, ct);

        if (notification is null) return Result<bool>.Failure("Notification not found");
        if (notification.ReadAt != null) return Result<bool>.Success(true);

        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> MarkAllAsReadAsync(int userId, CancellationToken ct)
    {
        var updated = await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsDeleted && n.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);

        return Result<bool>.Success(updated > 0);
    }

    // Features/Notifications/Services/NotificationService.cs
    public async Task CreateAsync(NotificationCreateRequest request, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            // Core fields
            RecipientId = request.RecipientId,
            Type = request.Type,
            Title = request.Title,
            Body = request.Body,

            // Delivery config
            Channel = request.Channel,
            Status = NotificationStatus.Pending, // Starts pending; worker updates to Sent/Delivered/Failed
            Priority = request.Priority,

            // Context references
            ComplaintId = request.ComplaintId,
            ChatRoomId = request.ChatRoomId,
            MessageId = request.MessageId,
            TriggeredByUserId = request.TriggeredByUserId,

            // Action & structured data
            ActionUrl = request.ActionUrl,
            PayloadJson = request.Payload != null
                ? System.Text.Json.JsonSerializer.Serialize(request.Payload)
                : null,

            // Tracing & audit
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N"),
            IsSystemGenerated = request.IsSystemGenerated,

            // Delivery tracking (initialized)
            DeliveryAttempts = 0,
            LastAttemptAt = null,
            LastErrorCode = null,
            LastErrorMessage = null,

            // Timestamps
            SentAt = null,      // Set by delivery worker
            DeliveredAt = null, // Set by delivery worker
            ReadAt = null,      // Set when user views
            CreatedAt = DateTime.UtcNow,

            // Soft delete
            IsDeleted = false
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);
    }
}