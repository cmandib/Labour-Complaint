// Models/Employer.cs
using LabourComplaint_Backend.Models.Constants;
using System.ComponentModel.DataAnnotations;

public class Employer
{
    public int Id { get; set; }
    [Required, StringLength(Constraints.NameMaxLength)] public required string LegalName { get; set; }
    [StringLength(Constraints.NameMaxLength)] public string? TradeName { get; set; }
    [StringLength(Constraints.RefCodeMax)] public string? RegistrationNumber { get; set; }
    [StringLength(300)] public string? Address { get; set; }
    [StringLength(100)] public string? Industry { get; set; }
    public double RiskScore { get; set; } = 0.0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}