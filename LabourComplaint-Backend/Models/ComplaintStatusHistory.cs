// Models/ComplaintStatusHistory.cs
using LabourComplaint_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class ComplaintStatusHistory
{
    public int Id { get; set; }
    [Required] public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    [Required] public ComplaintStatus OldStatus { get; set; }
    [Required] public ComplaintStatus NewStatus { get; set; }

    [Required] public int? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; } = null!;

    [Required] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    [StringLength(500)] public string? Reason { get; set; }
    public bool IsAutomated { get; set; }
    public DateTime? NextDueDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

}