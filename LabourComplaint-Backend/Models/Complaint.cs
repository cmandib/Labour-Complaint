// Models/Complaint.cs
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Constants;
using LabourComplaint_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class Complaint
{
    public int Id { get; set; }
    [Required, StringLength(Constraints.RefCodeMax)] public required string ReferenceNumber { get; set; }

    [Required] public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    [Required] public int DistrictId { get; set; }
    public District District { get; set; } = null!;

    public int? EmployerId { get; set; }
    public Employer? Employer { get; set; }

    [Required, StringLength(100)] public string Category { get; set; } = null!;
    [StringLength(100)] public string? SubCategory { get; set; }
    [Required] public PriorityLevel Severity { get; set; } = PriorityLevel.Normal;
    [Required] public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;

    [Required, StringLength(Constraints.DescriptionMax)] public required string Description { get; set; }
    [StringLength(200)] public string? WorkplaceName { get; set; }
    [StringLength(300)] public string? LocationAddress { get; set; }
    public double? GpsLat { get; set; }
    public double? GpsLng { get; set; }

    [Required] public int ConfidentialityLevel { get; set; } = 1; // 1=Public, 2=Inspector, 3=Admin

    public int? AssignedInspectorId { get; set; }
    public User? AssignedInspector { get; set; }

    public DateTime ResponseDueBy { get; set; }
    public DateTime ResolutionDueBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<ComplaintStatusHistory> StatusHistory { get; set; } = new List<ComplaintStatusHistory>();
    public ICollection<EvidenceItem> Evidence { get; set; } = new List<EvidenceItem>();
    public ChatRoom? ChatRoom { get; set; }
    public ICollection<Notification> RelatedNotifications { get; set; } = new List<Notification>();
}