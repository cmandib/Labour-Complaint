// Features/Complaints/Dtos/ListComplaintsResponseDto.cs
namespace LabourComplaint_Backend.Features.Complaints.Dtos;

/// <summary>
/// Paginated response for listing complaints
/// </summary>
public record ListComplaintsResponseDto
{
    public IEnumerable<ComplaintSummaryDto> Data { get; init; } = null!;
    public PaginationMetadata Pagination { get; init; } = null!;
}

public record PaginationMetadata
{
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
}