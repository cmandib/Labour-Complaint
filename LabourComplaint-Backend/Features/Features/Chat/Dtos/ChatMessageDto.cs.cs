// Features/Chat/Dtos/ChatMessageDto.cs
namespace LabourComplaint_Backend.Features.Chat.Dtos;

public record ChatMessageDto
{
    public int Id { get; init; }
    public int SenderId { get; init; }
    public string SenderName { get; init; } = null!;
    public string Content { get; init; } = null!;
    public string Direction { get; init; } = null!;
    public string? FileUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime SentAt { get; init; }
}