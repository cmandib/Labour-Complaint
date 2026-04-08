// Features/Auth/Dtos/LogoutDto.cs
namespace LabourComplaint_Backend.Features.Auth.Dtos;

public record LogoutDto
{
    public string? Reason { get; init; }
}