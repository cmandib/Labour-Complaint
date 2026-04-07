// Models/OutboxMessage.cs
using LabourComplaint_Backend.Models.Constants;
using System.ComponentModel.DataAnnotations;

public class OutboxMessage
{
    public int Id { get; set; }
    [Required, StringLength(100)] public required string EventType { get; set; }
    [Required, StringLength(Constraints.JsonMax)] public required string PayloadJson { get; set; }
    [StringLength(100)] public string? AggregateType { get; set; }
    public int? AggregateId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    [StringLength(500)] public string? ErrorDetails { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

}