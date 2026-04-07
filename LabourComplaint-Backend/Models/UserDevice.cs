// Models/UserDevice.cs
using System.ComponentModel.DataAnnotations;

public class UserDevice
{
    public int Id { get; set; }
    [Required] public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, StringLength(255)] public required string DeviceToken { get; set; }
    [Required, StringLength(50)] public required string DeviceType { get; set; }
    [StringLength(100)] public string? DeviceModel { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public int? SubscribedDistrictId { get; set; }
    public District? SubscribedDistrict { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

}