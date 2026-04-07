using System.ComponentModel.DataAnnotations;
using LabourComplaint_Backend.Models.Constants;

namespace LabourComplaint_Backend.Features.Complaints.Dtos;

public record ComplaintCreateDto
{
    [Required] public int DistrictId { get; init; }

    [Required, StringLength(100)] public required string Category { get; init; }
    [StringLength(100)] public string? SubCategory { get; init; }

    [Required, StringLength(Constraints.DescriptionMax, MinimumLength = 10)]
    public required string Description { get; init; }

    [StringLength(200)] public string? WorkplaceName { get; init; }
    [StringLength(300)] public string? LocationAddress { get; init; }
    public double? GpsLat { get; init; }
    public double? GpsLng { get; init; }

    [Range(1, 4)] public int Severity { get; init; } = 2; // Default: Normal
    [Range(1, 3)] public int ConfidentialityLevel { get; init; } = 1; // Default: Public

    // Initial evidence (metadata only - file upload handled separately)
    public List<EvidenceUploadDto>? InitialEvidence { get; init; }
}