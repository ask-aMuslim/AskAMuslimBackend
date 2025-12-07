using AskAMuslimBackend.Application.Authentication.Commands.Login;
using AskAMuslimBackend.Application.Authentication.Commands.Register;
using AskAMuslimBackend.Application.Authentication.Commands.LoginWithGoogle;
using AskAMuslimBackend.Application.Authentication.Commands.LoginWithFacebook;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Web.Infrastructure;

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

    public async Task<IResult> Register(
        ISender sender, 
        RegisterCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> Login(
        ISender sender, 
        LoginCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> LoginWithGoogle(
        ISender sender,
        LoginWithGoogleCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> LoginWithFacebook(
        ISender sender,
        LoginWithFacebookCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
