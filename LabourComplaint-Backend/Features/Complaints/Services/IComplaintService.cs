using LabourComplaint_Backend.Features.Complaints.Dtos;
using LabourComplaint_Backend.Shared.Results;

namespace LabourComplaint_Backend.Features.Complaints.Services;

public interface IComplaintService
{
    Task<Result<ComplaintResponseDto>> CreateComplaintAsync(
        ComplaintCreateDto dto, int reporterUserId, CancellationToken ct);

    Task<Result<ComplaintResponseDto>> GetComplaintByReferenceAsync(
        string referenceNumber, int requestingUserId, CancellationToken ct);

    Task<Result<List<ComplaintResponseDto>>> ListComplaintsAsync(
        int? districtId, int? reporterId, string? status,
        int page = 1, int limit = 20, CancellationToken ct = default);
}