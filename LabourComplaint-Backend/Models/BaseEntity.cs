// Models/BaseEntity.cs
using LabourComplaint_Backend.Models.Constants;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabourComplaint_Backend.Models;

public abstract class BaseEntity
{
    public int Id { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Soft delete + multi-tenant ready
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Optional: for multi-tenant SaaS later
    public int? TenantId { get; set; }
}

// For queries that need district context WITHOUT joins
public abstract class DistrictScopedEntity : BaseEntity
{
    [Required]
    public int DistrictId { get; set; } // Denormalized for fast API filtering

    // Optional: cache district name for read APIs (reduces joins)
    [StringLength(Constraints.NameMaxLength)]
    public string? DistrictNameCache { get; set; }
}