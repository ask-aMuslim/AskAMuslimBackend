using AskAMuslimBackend.Application.Common.Models;

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

    public static IResult ToCreatedResult<T>(this Result<T> result, string uri)
    {
        if (result.Succeeded && result.Data is not null)
        {
            return Results.Created(uri, result.Data);
        }

        return Results.BadRequest(new { errors = result.Errors });
    }
}
