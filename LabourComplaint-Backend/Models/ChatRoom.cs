// Models/ChatRoom.cs
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class ChatRoom
{
    public int Id { get; set; }
    [Required] public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    [Required] public int CitizenId { get; set; }
    public User Citizen { get; set; } = null!;

    [Required] public int InspectorId { get; set; }
    public User Inspector { get; set; } = null!;

    public int DistrictId { get; set; }
    public District District { get; set; } = null!;

    [StringLength(200)] public string? Subject { get; set; }
    [Required] public ChatRoomStatus Status { get; set; } = ChatRoomStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public bool IsDeleted { get; set; }
    [StringLength(400)] public string? LastMessagePreview { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Notification> RelatedNotifications { get; set; } = new List<Notification>();
}