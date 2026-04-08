// Features/Complaints/Dtos/AssignInspectorDto.cs
namespace LabourComplaint_Backend.Features.Complaints.Dtos;

public record AssignInspectorDto
{
    public int InspectorId { get; init; }
    public string? Reason { get; init; } = "Manual assignment";
}