using LabourComplaint_Backend.Features.Notifications.Dtos;
using LabourComplaint_Backend.Models.Enums;
using LabourComplaint_Backend.Shared.Results;

namespace LabourComplaint_Backend.Features.Notifications.Services;

public interface INotificationService
{
    Task<Result<NotificationResponseDto>> GetNotificationsAsync(
        int userId, bool unreadOnly = false, int page = 1, int limit = 20, CancellationToken ct = default);

    Task<Result<bool>> MarkAsReadAsync(int notificationId, int userId, CancellationToken ct);
    Task<Result<bool>> MarkAllAsReadAsync(int userId, CancellationToken ct);

    Task CreateAsync(NotificationCreateRequest request, CancellationToken ct = default);
}

public record NotificationCreateRequest
{
    public int RecipientId { get; init; }
    public NotificationType Type { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;

    // Delivery config
    public NotificationChannel Channel { get; init; } = NotificationChannel.InApp;
    public PriorityLevel Priority { get; init; } = PriorityLevel.Normal;

    // Context references (optional but recommended)
    public int? ComplaintId { get; init; }
    public int? ChatRoomId { get; init; }
    public int? MessageId { get; init; }
    public int? TriggeredByUserId { get; init; }

    // Action & payload
    public string? ActionUrl { get; init; }
    public object? Payload { get; init; } // Will be serialized to PayloadJson

    // Tracing
    public string? CorrelationId { get; init; }
    public bool IsSystemGenerated { get; init; } = true;
}