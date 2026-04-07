using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Features.Complaints.Dtos;
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using LabourComplaint_Backend.Shared.Helpers;
using LabourComplaint_Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace LabourComplaint_Backend.Features.Complaints.Services;

public class ComplaintService : IComplaintService
{
    private readonly ApplicationDbContext _db;

    public ComplaintService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ComplaintResponseDto>> CreateComplaintAsync(
        ComplaintCreateDto dto, int reporterUserId, CancellationToken ct)
    {
        // 1. Verify district exists and is active
        var district = await _db.Districts
            .FirstOrDefaultAsync(d => d.Id == dto.DistrictId && !d.IsDeleted, ct);

        if (district is null)
            return Result<ComplaintResponseDto>.Failure("Invalid district");

        // 2. Create complaint entity
        var complaint = new Complaint
        {
            ReferenceNumber = ReferenceNumberGenerator.Generate(),
            ReporterId = reporterUserId,
            DistrictId = dto.DistrictId,
            Category = dto.Category,
            SubCategory = dto.SubCategory,
            Description = dto.Description,
            WorkplaceName = dto.WorkplaceName,
            LocationAddress = dto.LocationAddress,
            GpsLat = dto.GpsLat,
            GpsLng = dto.GpsLng,
            Severity = (PriorityLevel)dto.Severity,
            ConfidentialityLevel = dto.ConfidentialityLevel,
            Status = ComplaintStatus.Submitted,
            ResponseDueBy = DateTime.UtcNow.AddHours(24), // SLA: 24h first response
            ResolutionDueBy = DateTime.UtcNow.AddDays(7)   // SLA: 7 days resolution
        };

        _db.Complaints.Add(complaint);

        // 3. Save to get ID for evidence linking
        await _db.SaveChangesAsync(ct);

        // 4. Process initial evidence (metadata only - actual files handled by storage service)
        if (dto.InitialEvidence?.Any() == true)
        {
            var evidenceItems = dto.InitialEvidence.Select(e => new EvidenceItem
            {
                ComplaintId = complaint.Id,
                UploadedByUserId = reporterUserId,
                Type = DetermineEvidenceType(e.MimeType),
                StoragePath = e.PreSignedUrl ?? $"/temp/{complaint.ReferenceNumber}/{e.FileName}",
                FileSizeBytes = e.FileSizeBytes,
                MimeType = e.MimeType,
                Description = e.Description,
                Visibility = EvidenceVisibility.PublicToCase,
                IsVerified = false
            }).ToList();

            _db.EvidenceItems.AddRange(evidenceItems);
            await _db.SaveChangesAsync(ct);
        }

        // 5. Log status change in history
        _db.ComplaintStatusHistories.Add(new ComplaintStatusHistory
        {
            ComplaintId = complaint.Id,
            OldStatus = ComplaintStatus.Draft, // Conceptual start
            NewStatus = ComplaintStatus.Submitted,
            ChangedByUserId = reporterUserId,
            Reason = "Initial submission",
            IsAutomated = false
        });

        // 6. Queue assignment event via outbox
        _db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "ComplaintSubmitted",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ComplaintId = complaint.Id,
                ReferenceNumber = complaint.ReferenceNumber,
                DistrictId = complaint.DistrictId,
                Category = complaint.Category,
                Severity = complaint.Severity.ToString()
            }),
            DistrictId = complaint.DistrictId
        });

        await _db.SaveChangesAsync(ct);

        // 7. Build response DTO
        var response = await BuildComplaintResponseDto(complaint, ct);
        return Result<ComplaintResponseDto>.Success(response);
    }

    public async Task<Result<ComplaintResponseDto>> GetComplaintByReferenceAsync(
        string referenceNumber, int requestingUserId, CancellationToken ct)
    {
        var complaint = await _db.Complaints
            .Include(c => c.Reporter)
            .Include(c => c.District)
            .Include(c => c.Evidence)
            .Include(c => c.ChatRoom)
            .FirstOrDefaultAsync(c => c.ReferenceNumber == referenceNumber && !c.IsDeleted, ct);

        if (complaint is null)
            return Result<ComplaintResponseDto>.Failure("Complaint not found");

        // Authorization check: can this user view this complaint?
        if (!await CanUserViewComplaintAsync(complaint, requestingUserId, ct))
            return Result<ComplaintResponseDto>.Failure("Access denied");

        var response = await BuildComplaintResponseDto(complaint, ct);
        return Result<ComplaintResponseDto>.Success(response);
    }

    public async Task<Result<PaginatedResult<ComplaintSummaryDto>>> ListComplaintsAsync(
    int? districtId, int? reporterId, string? status,
    int page = 1, int limit = 20, CancellationToken ct = default)
    {
        var query = _db.Complaints
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (districtId.HasValue)
            query = query.Where(c => c.DistrictId == districtId.Value);
        if (reporterId.HasValue)
            query = query.Where(c => c.ReporterId == reporterId.Value);
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ComplaintStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(c => c.Status == parsedStatus);
        }

        // Get total count BEFORE pagination
        var totalCount = await query.CountAsync(ct);

        var summaries = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ComplaintSummaryDto
            {
                ReferenceNumber = c.ReferenceNumber,
                Category = c.Category,
                SubCategory = c.SubCategory,
                Status = c.Status.ToString(),
                Severity = c.Severity.ToString(),
                CreatedAt = c.CreatedAt,
                WorkplaceName = c.WorkplaceName,
                DistrictId = c.DistrictId
            })
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        // Return wrapped result with pagination metadata
        var paginated = new PaginatedResult<ComplaintSummaryDto>(summaries, totalCount, page, limit);
        return Result<PaginatedResult<ComplaintSummaryDto>>.Success(paginated);
    }
    // Helper: Build response DTO from entity
    private async Task<ComplaintResponseDto> BuildComplaintResponseDto(Complaint c, CancellationToken ct)
    {
        var evidenceCount = await _db.EvidenceItems
            .CountAsync(e => e.ComplaintId == c.Id && !e.IsDeleted, ct);

        return new ComplaintResponseDto
        {
            ReferenceNumber = c.ReferenceNumber,
            Category = c.Category,
            SubCategory = c.SubCategory,
            Description = c.Description,
            WorkplaceName = c.WorkplaceName,
            LocationAddress = c.LocationAddress,
            Status = c.Status,
            Severity = c.Severity,
            ConfidentialityLevel = c.ConfidentialityLevel,
            ReporterUsername = c.Reporter.Username,
            DistrictId = c.DistrictId,
            DistrictName = c.District.Name,
            CreatedAt = c.CreatedAt,
            ResponseDueBy = c.ResponseDueBy,
            ResolutionDueBy = c.ResolutionDueBy,
            EvidenceCount = evidenceCount,
            HasChatRoom = c.ChatRoom != null
        };
    }

    // Helper: Authorization logic
    private async Task<bool> CanUserViewComplaintAsync(Complaint complaint, int userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);
        if (user is null) return false;

        // Admins can view all
        if (user.Role == UserRole.Admin) return true;

        // Reporter can view their own
        if (complaint.ReporterId == userId) return true;

        // Assigned inspector can view
        if (complaint.AssignedInspectorId == userId) return true;

        // Inspector in same district can view non-confidential complaints
        if (user.Role == UserRole.Inspector &&
            user.AssignedDistrictId == complaint.DistrictId &&
            complaint.ConfidentialityLevel <= 2)
        {
            return true;
        }

        return false;
    }

    // Helper: Determine evidence type from MIME
    private EvidenceType DetermineEvidenceType(string mimeType)
    {
        return mimeType switch
        {
            var m when m.StartsWith("image/") => EvidenceType.Photo,
            var m when m.StartsWith("video/") => EvidenceType.Video,
            var m when m.StartsWith("audio/") => EvidenceType.Audio,
            var m when m is "application/pdf" or "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => EvidenceType.Document,
            _ => EvidenceType.Link
        };
    }
}