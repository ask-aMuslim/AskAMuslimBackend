using AskAMuslimBackend.Application.Authentication.Commands.Login;
using AskAMuslimBackend.Application.Authentication.Commands.Register;
using AskAMuslimBackend.Application.Authentication.Commands.LoginWithGoogle;
using AskAMuslimBackend.Application.Authentication.Commands.LoginWithFacebook;
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

        groupBuilder.MapPost(LoginWithGoogle, "login/google")
            .AllowAnonymous()
            .WithName("LoginWithGoogle")
            .WithSummary("Login with Google")
            .WithDescription("Accepts a Google ID token, verifies it and returns a JWT token");

        groupBuilder.MapPost(LoginWithFacebook, "login/facebook")
            .AllowAnonymous()
            .WithName("LoginWithFacebook")
            .WithSummary("Login with Facebook")
            .WithDescription("Accepts a Facebook access token, verifies it and returns a JWT token");
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

    public async Task<Results<Ok<AuthenticationResponse>, UnauthorizedHttpResult>> LoginWithGoogle(
        ISender sender,
        LoginWithGoogleCommand command)
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

    public async Task<Results<Ok<AuthenticationResponse>, UnauthorizedHttpResult>> LoginWithFacebook(
        ISender sender,
        LoginWithFacebookCommand command)
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
