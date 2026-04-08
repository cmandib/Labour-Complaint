// Features/Complaints/Dtos/AssignResponseDto.cs
namespace LabourComplaint_Backend.Features.Complaints.Dtos;

public record AssignResponseDto
{
    public string ReferenceNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public int AssignedInspectorId { get; init; }
    public string AssignedInspectorName { get; init; } = null!;
    public int ChatRoomId { get; init; }
    public DateTime AssignedAt { get; init; }
}