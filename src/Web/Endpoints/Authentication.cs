using AskAMuslimBackend.Application.Authentication.Commands.Login;
using AskAMuslimBackend.Application.Authentication.Commands.Register;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Authentication : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "register")
            .AllowAnonymous()
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account with the specified role (Admin, Student, Instructor, NonMuslim)");

        groupBuilder.MapPost(Login, "login")
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Login user")
            .WithDescription("Authenticates a user and returns a JWT token");
    }

    public async Task<Results<Ok<AuthenticationResponse>, BadRequest<string[]>>> Register(
        ISender sender, 
        RegisterCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new[] { ex.Message });
        }
    }

    public async Task<Results<Ok<AuthenticationResponse>, UnauthorizedHttpResult>> Login(
        ISender sender, 
        LoginCommand command)
    {
        try
        {
            var result = await sender.Send(command);
            return TypedResults.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
    }
}
