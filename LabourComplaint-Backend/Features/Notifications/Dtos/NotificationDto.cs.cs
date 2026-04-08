// Features/Notifications/Dtos/NotificationDto.cs
namespace LabourComplaint_Backend.Features.Notifications.Dtos;

public record NotificationDto
{
    public int Id { get; init; }
    public string Type { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public string? ActionUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record NotificationResponseDto
{
    public IEnumerable<NotificationDto> Items { get; init; } = null!;
    public int UnreadCount { get; init; }
}