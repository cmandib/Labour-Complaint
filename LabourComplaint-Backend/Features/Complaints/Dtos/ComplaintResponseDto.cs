using LabourComplaint_Backend.Models.Enums;

namespace LabourComplaint_Backend.Features.Complaints.Dtos;

public record ComplaintResponseDto
{
    public required string ReferenceNumber { get; init; }
    public required string Category { get; init; }
    public string? SubCategory { get; init; }
    public required string Description { get; init; }
    public string? WorkplaceName { get; init; }
    public string? LocationAddress { get; init; }

    public required ComplaintStatus Status { get; init; }
    public required PriorityLevel Severity { get; init; }
    public int ConfidentialityLevel { get; init; }

    public required string ReporterUsername { get; init; }
    public int DistrictId { get; init; }
    public required string DistrictName { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime ResponseDueBy { get; init; }
    public DateTime ResolutionDueBy { get; init; }

    public int EvidenceCount { get; init; }
    public bool HasChatRoom { get; init; }

    // Computed for UI
    public string StatusDisplay => Status switch
    {
        ComplaintStatus.Submitted => "Submitted - Awaiting Assignment",
        ComplaintStatus.Assigned => "Assigned to Inspector",
        ComplaintStatus.UnderReview => "Under Review",
        ComplaintStatus.EvidenceRequested => "Additional Evidence Requested",
        ComplaintStatus.Resolved => "Resolved",
        ComplaintStatus.Closed => "Closed",
        _ => Status.ToString()
    };
}