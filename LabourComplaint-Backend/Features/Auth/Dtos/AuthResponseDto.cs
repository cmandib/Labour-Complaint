// Features/Auth/Dtos/AuthResponseDto.cs
using LabourComplaint_Backend.Models.Enums;

public record AuthResponseDto
{
    public int UserId { get; init; }
    public string Username { get; init; } = null!;
    public string? Email { get; init; }
    public UserRole Role { get; init; }
    public string Token { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
}