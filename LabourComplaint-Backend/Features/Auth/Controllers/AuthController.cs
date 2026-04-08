// Features/Auth/Controllers/AuthController.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using LabourComplaint_Backend.Features.Auth.Dtos;
using LabourComplaint_Backend.Features.Auth.Services;
using LabourComplaint_Backend.Features.Auth.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabourComplaint_Backend.Features.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;

    public AuthController(
        IAuthService service,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator)
    {
        _service = service;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto dto, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var result = await _service.RegisterAsync(dto, ct);

        return result.Match<ActionResult<AuthResponseDto>>(
            onSuccess: response => CreatedAtAction(nameof(Login), new { }, response),
            onFailure: error => BadRequest(new { error })
        );
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto dto, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return BadRequest(validation.ToDictionary());

        var result = await _service.LoginAsync(dto, ct);

        return result.Match<ActionResult<AuthResponseDto>>(
            onSuccess: response => Ok(response),
            onFailure: error => Unauthorized(new { error })
        );
    }

    /// <summary>
    /// Logout and blacklist the current JWT token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout(
        [FromBody] LogoutDto? dto, CancellationToken ct)
    {
        // 🔑 Get user ID: try mapped claim first, then fallback to raw "sub"
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        // 🔑 Extract full token from Authorization header
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();

        var fullToken = authHeader["Bearer ".Length..].Trim();

        // 🔑 Get token identifier: prefer JTI claim, fallback to SHA256 hash of full token
        var tokenIdentifier = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(tokenIdentifier))
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fullToken));
            tokenIdentifier = Convert.ToHexString(hashBytes);
        }

        // 🔑 Call service to blacklist
        var result = await _service.LogoutAsync(userId, tokenIdentifier, fullToken, dto?.Reason, ct);

        return result.Match<ActionResult>(
            onSuccess: _ => Ok(new { message = "Logged out successfully" }),
            onFailure: error => BadRequest(new { error })
        );
    }
}