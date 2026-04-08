// Models/OutboxMessage.cs
using System.ComponentModel.DataAnnotations;
using LabourComplaint_Backend.Models.Constants;

namespace LabourComplaint_Backend.Models;

public class OutboxMessage : BaseEntity  // BaseEntity already has Id, CreatedAt, IsDeleted, etc.
{
    [Required, StringLength(100)] public required string EventType { get; set; } = null!;

    [Required, StringLength(Constraints.JsonMax)] public required string PayloadJson { get; set; } = null!;

    [StringLength(100)] public string? AggregateType { get; set; }
    public int? AggregateId { get; set; }

    // For district-filtered event processing
    public int? DistrictId { get; set; }

    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    [StringLength(500)] public string? ErrorDetails { get; set; }
    [StringLength(100)] public string? CorrelationId { get; set; }
}