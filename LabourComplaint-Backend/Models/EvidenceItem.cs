// Models/EvidenceItem.cs
using LabourComplaint_Backend.Models.Constants;
using LabourComplaint_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class EvidenceItem
{
    public int Id { get; set; }
    [Required] public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    [Required] public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    [Required] public EvidenceType Type { get; set; }
    [Required, StringLength(Constraints.PathMax)] public required string StoragePath { get; set; }
    public long FileSizeBytes { get; set; }
    [StringLength(100)] public string? MimeType { get; set; }
    [StringLength(100)] public string? Checksum { get; set; }
    [StringLength(500)] public string? Description { get; set; }

    [Required] public EvidenceVisibility Visibility { get; set; } = EvidenceVisibility.PublicToCase;
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}