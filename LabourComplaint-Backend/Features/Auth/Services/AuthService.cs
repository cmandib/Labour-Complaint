// Features/Auth/Services/AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LabourComplaint_Backend.Data;
using LabourComplaint_Backend.Features.Auth.Dtos;
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using LabourComplaint_Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LabourComplaint_Backend.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _signingKey;

    public AuthService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;

        var secretKey = _config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct)
    {
        // Check duplicates
        if (await _db.Users.AnyAsync(u =>
            (u.Email == dto.Email || u.Username == dto.Username) && !u.IsDeleted, ct))
        {
            return Result<AuthResponseDto>.Failure("User with this email or username already exists");
        }

        // Validate district for inspectors
        if (dto.Role == UserRole.Inspector && dto.AssignedDistrictId.HasValue)
        {
            var district = await _db.Districts
                .FirstOrDefaultAsync(d => d.Id == dto.AssignedDistrictId.Value && !d.IsDeleted, ct);
            if (district is null)
                return Result<AuthResponseDto>.Failure("Invalid district assigned");
        }

        // Hash password & create user
        var user = new User
        {
            Username = dto.Username.Trim(),
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            AssignedDistrictId = dto.AssignedDistrictId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = GenerateJwtToken(user);
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var mins) ? mins : 60;

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        });
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant() && !u.IsDeleted, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Result<AuthResponseDto>.Failure("Invalid email or password");

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure("Account is deactivated");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var token = GenerateJwtToken(user);
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var mins) ? mins : 60;

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        });
    }

    // Features/Auth/Services/AuthService.cs
    public async Task<Result<bool>> LogoutAsync(int userId, string tokenIdentifier, string fullToken, string? reason, CancellationToken ct)
    {
        // Check if already blacklisted (idempotent)
        if (await _db.BlacklistedTokens.AnyAsync(t =>
            t.TokenIdentifier == tokenIdentifier && !t.IsDeleted, ct))
        {
            return Result<bool>.Success(true);
        }

        //  Use GetTokenExpiry to parse exact expiry from the JWT
        var expiresAt = GetTokenExpiry(fullToken)
            ?? DateTime.UtcNow.AddMinutes(int.TryParse(_config["Jwt:ExpiryMinutes"], out var mins) ? mins : 60);

        var blacklistEntry = new BlacklistedToken
        {
            TokenIdentifier = tokenIdentifier,
            UserId = userId,
            ExpiresAt = expiresAt,
            Reason = reason ?? "User logout"
        };

        _db.BlacklistedTokens.Add(blacklistEntry);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    //  Your helper — now actively used!
    private static DateTime? GetTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo; // Returns the exact 'exp' claim value
        }
        catch
        {
            return null; // Fallback to config-based expiry
        }
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("role", user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ✅ Unique token ID
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

        if (user.Role == UserRole.Inspector && user.AssignedDistrictId.HasValue)
            claims.Add(new Claim("district_id", user.AssignedDistrictId.Value.ToString()));

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var issuer = _config["Jwt:Issuer"] ?? "LabourComplaint-API";
        var audience = _config["Jwt:Audience"] ?? "LabourComplaint-Client";
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var mins) ? mins : 60;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}