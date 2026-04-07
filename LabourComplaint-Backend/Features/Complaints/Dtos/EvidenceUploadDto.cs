using System.ComponentModel.DataAnnotations;

namespace LabourComplaint_Backend.Features.Complaints.Dtos;

public record EvidenceUploadDto
{
    [Required] public string FileName { get; init; } = null!;
    [Required] public string MimeType { get; init; } = null!;
    public long FileSizeBytes { get; init; }

    // Client-side base64 for small files, or pre-signed URL for large
    public string? Base64Content { get; init; }
    public string? PreSignedUrl { get; init; }

    [StringLength(500)] public string? Description { get; init; }
}