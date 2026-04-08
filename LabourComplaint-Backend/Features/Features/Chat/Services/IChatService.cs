// Features/Chat/Services/IChatService.cs
using LabourComplaint_Backend.Features.Chat.Dtos;
using LabourComplaint_Backend.Shared.Results;

namespace LabourComplaint_Backend.Features.Chat.Services;
public interface IChatService
{
    Task<Result<IEnumerable<ChatMessageDto>>> GetMessagesAsync(
        int chatRoomId, int requestingUserId, int page = 1, int limit = 50, CancellationToken ct = default);

    Task<Result<ChatMessageDto>> SendMessageAsync(
        int chatRoomId, int senderId, SendMessageDto dto, CancellationToken ct);
    Task<Result<int>> MarkMessagesReadAsync(
        int chatRoomId, int userId, CancellationToken ct);
}