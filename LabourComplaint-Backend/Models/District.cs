// Models/District.cs
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Constants;
using System.ComponentModel.DataAnnotations;

public class District
{
    public int Id { get; set; }
    [Required, StringLength(Constraints.NameMaxLength, MinimumLength = 2)] public required string Name { get; set; }
    [StringLength(Constraints.NameMaxLength)] public string? ExternalCode { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    [StringLength(50)] public string? TimeZone { get; set; } = "UTC";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<User> AssignedInspectors { get; set; } = new List<User>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    public ICollection<UserDevice> SubscribedDevices { get; set; } = new List<UserDevice>();
}