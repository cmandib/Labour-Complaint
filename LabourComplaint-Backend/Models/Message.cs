// Models/Message.cs
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Constants;
using LabourComplaint_Backend.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class Message
{
    public int Id { get; set; }
    [Required] public int ChatRoomId { get; set; }
    public ChatRoom ChatRoom { get; set; } = null!;

    [Required] public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    [Required, StringLength(Constraints.ContentMax, MinimumLength = 1)] public required string Content { get; set; }
    [Required] public MessageDirection Direction { get; set; }

    [StringLength(Constraints.UrlMax)] public string? FileUrl { get; set; }
    [StringLength(255)] public string? FileName { get; set; }
    public long? FileSizeBytes { get; set; }
    [StringLength(100)] public string? FileMimeType { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public int? ParentMessageId { get; set; }
    public Message? ParentMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();

    public bool ShouldNotify { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    public ICollection<Notification> GeneratedNotifications { get; set; } = new List<Notification>();
}