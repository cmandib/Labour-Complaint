// Features/Auth/Services/IAuthService.cs
using LabourComplaint_Backend.Features.Auth.Dtos;
using LabourComplaint_Backend.Shared.Results;

namespace LabourComplaint_Backend.Features.Auth.Services;
public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct);
    Task<Result<bool>> LogoutAsync(int userId, string tokenIdentifier, string fullToken, string? reason, CancellationToken ct);
}