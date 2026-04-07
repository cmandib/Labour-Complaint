// Features/Complaints/Dtos/ComplaintSummaryDto.cs
namespace LabourComplaint_Backend.Features.Complaints.Dtos;

/// <summary>
/// Summary view of a complaint for list endpoints
/// </summary>
public record ComplaintSummaryDto
{
    public string ReferenceNumber { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string SubCategory { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string Severity { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public string? WorkplaceName { get; init; }
    public int DistrictId { get; init; }
}