// Models/BlacklistedToken.cs
using System.ComponentModel.DataAnnotations;

namespace LabourComplaint_Backend.Models;

/// <summary>
/// Stores JWT tokens that have been explicitly invalidated (e.g., via logout)
/// before their natural expiration. Used to prevent token reuse.
/// </summary>
public class BlacklistedToken
{
    [Key]
    public int Id { get; set; }
    [Required, StringLength(500)]
    public required string TokenIdentifier { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime BlacklistedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    [StringLength(100)]
    public string? Reason { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}