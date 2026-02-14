using System.IdentityModel.Tokens.Jwt;
using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Web.Infrastructure;

/// <summary>
/// Middleware to validate JWT tokens against the blacklist.
/// Rejects requests with blacklisted (logged out) tokens.
/// </summary>
public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService tokenBlacklistService)
    {
        // Check if the user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Extract the JTI (JWT ID) claim from the token
            var jtiClaim = context.User.FindFirst(JwtRegisteredClaimNames.Jti);

            if (jtiClaim != null)
            {
                var tokenId = jtiClaim.Value;

                // Check if the token is blacklisted
                var isBlacklisted = await tokenBlacklistService.IsTokenBlacklistedAsync(tokenId);

                if (isBlacklisted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Token has been invalidated. Please log in again."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to add the JWT blacklist middleware to the pipeline.
/// </summary>
public static class JwtBlacklistMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtBlacklist(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtBlacklistMiddleware>();
    }
}
