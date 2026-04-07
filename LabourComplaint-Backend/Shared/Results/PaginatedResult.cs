// LabourComplaint_Backend/Shared/Results/PaginatedResult.cs
namespace LabourComplaint_Backend.Shared.Results;

/// <summary>
/// Wrapper for paginated query results with metadata
/// </summary>
public record PaginatedResult<T>(IEnumerable<T> Items, int Total, int Page, int Limit)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)Limit);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}