using AskAMuslimBackend.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Web.Infrastructure;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.Succeeded)
        {
            return Results.NoContent();
        }

        return Results.BadRequest(new { errors = result.Errors });
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.Succeeded)
        {
            // Return the full Result wrapper with succeeded, errors, and data
            return Results.Ok(result);
        }

        // Return the full Result wrapper with error information
        return Results.BadRequest(result);
    }

    public static IResult ToAuthenticationHttpResult(this Result<AuthenticationResponse> result, HttpContext httpContext)
    {
        if (result.Succeeded && result.Data != null && !string.IsNullOrEmpty(result.Data.RefreshToken))
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.Data.RefreshTokenExpiresAt,
                Path = "/api/Authentication/refresh-token"
            };

            httpContext.Response.Cookies.Append("refreshToken", result.Data.RefreshToken, cookieOptions);
        }

        return result.ToHttpResult();
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, string uri)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Results.Created(uri, result.Data);
        }

        return Results.BadRequest(new { errors = result.Errors });
    }
}
