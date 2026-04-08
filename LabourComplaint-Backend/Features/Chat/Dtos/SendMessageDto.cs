// Features/Chat/Dtos/SendMessageDto.cs
namespace LabourComplaint_Backend.Features.Chat.Dtos;

public record SendMessageDto
{
    public string Content { get; init; } = null!;
    public string? FileUrl { get; init; }
    public string? FileName { get; init; }
    public long? FileSizeBytes { get; init; }
    public string? FileMimeType { get; init; }
}