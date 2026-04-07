// Models/Notification.cs
using LabourComplaint_Backend.Models.Constants;
using LabourComplaint_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class Notification
{
    public int Id { get; set; }
    [Required] public int RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    [Required] public NotificationType Type { get; set; }
    [Required, StringLength(Constraints.TitleMax)] public required string Title { get; set; }
    [Required, StringLength(Constraints.BodyMax)] public required string Body { get; set; }

    [Required] public NotificationChannel Channel { get; set; }
    [Required] public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    [Required] public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    [StringLength(Constraints.UrlMax)] public string? ActionUrl { get; set; }
    [StringLength(Constraints.JsonMax)] public string? PayloadJson { get; set; }

    public int? TriggeredByUserId { get; set; }
    public User? TriggeredByUser { get; set; }

    public int? ChatRoomId { get; set; }
    public ChatRoom? ChatRoom { get; set; }

    public int? MessageId { get; set; }
    public Message? Message { get; set; }

    public int? ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    public int DeliveryAttempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    [StringLength(Constraints.ErrorCodeMax)] public string? LastErrorCode { get; set; }
    [StringLength(500)] public string? LastErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsSystemGenerated { get; set; }
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}