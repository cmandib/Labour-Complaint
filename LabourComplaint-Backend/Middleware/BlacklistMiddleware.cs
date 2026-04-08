// Middleware/BlacklistMiddleware.cs
using LabourComplaint_Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace LabourComplaint_Backend.Middleware;

public class BlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public BlacklistMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Extract token identifier (JTI or hash)
            var tokenIdentifier = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(tokenIdentifier))
            {
                // Fallback: hash the full token from header
                var authHeader = context.Request.Headers.Authorization.ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader["Bearer ".Length..].Trim();
                    using var sha256 = SHA256.Create();
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
                    tokenIdentifier = Convert.ToHexString(hashBytes);
                }
            }

            if (!string.IsNullOrEmpty(tokenIdentifier))
            {
                // Check blacklist (use scoped DB context)
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var isBlacklisted = await db.BlacklistedTokens
                    .AnyAsync(t => t.TokenIdentifier == tokenIdentifier
                        && !t.IsDeleted
                        && t.ExpiresAt > DateTime.UtcNow);

                if (isBlacklisted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Token has been invalidated" });
                    return;
                }
            }
        }

        await _next(context);
    }
}