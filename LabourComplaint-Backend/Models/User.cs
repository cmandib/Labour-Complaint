// Models/User.cs
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Constants;
using LabourComplaint_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class User
{
    public int Id { get; set; }
    [Required, StringLength(Constraints.UsernameMax, MinimumLength = Constraints.UsernameMin)] public required string Username { get; set; }
    [Required, StringLength(Constraints.PasswordHashLength)] public required string PasswordHash { get; set; }
    [Required] public UserRole Role { get; set; } = UserRole.Citizen;

    [StringLength(Constraints.NameMaxLength)] public string? FullName { get; set; }
    [StringLength(Constraints.EmailMax), EmailAddress] public string? Email { get; set; }
    [StringLength(Constraints.PhoneMax), Phone] public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
    [StringLength(Constraints.JsonMax)] public string? NotificationPreferencesJson { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // District assignment (nullable: citizens don't need one)
    public int? AssignedDistrictId { get; set; }
    public District? AssignedDistrict { get; set; }

    // Relationships
    public ICollection<Complaint> ReportedComplaints { get; set; } = new List<Complaint>();
    public ICollection<Complaint> InspectedComplaints { get; set; } = new List<Complaint>();
    public ICollection<ChatRoom> InitiatedChats { get; set; } = new List<ChatRoom>();
    public ICollection<ChatRoom> AssignedChats { get; set; } = new List<ChatRoom>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
    public ICollection<ComplaintStatusHistory> StatusChanges { get; set; } = new List<ComplaintStatusHistory>();
    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
    public ICollection<EvidenceItem> UploadedEvidence { get; set; } = new List<EvidenceItem>();
}