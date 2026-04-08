// Features/Auth/Dtos/RegisterDto.cs
using LabourComplaint_Backend.Models.Enums;

namespace LabourComplaint_Backend.Features.Auth.Dtos;

public record RegisterDto
{
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string ConfirmPassword { get; init; } = null!;
    public UserRole Role { get; init; } = UserRole.Citizen;
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public int? AssignedDistrictId { get; init; } // Required for Inspectors
}