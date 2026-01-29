using AskAMuslimBackend.Application.Authentication.Commands.Login;
using AskAMuslimBackend.Application.Authentication.Commands.Register;
using AskAMuslimBackend.Application.Authentication.Commands.LoginWithGoogle;
using AskAMuslimBackend.Application.Authentication.Commands.LoginWithFacebook;
using AskAMuslimBackend.Application.Authentication.Commands.ForgotPassword;
using AskAMuslimBackend.Application.Authentication.Commands.ResetPassword;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Authentication : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "register")
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithName("Register")
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account with the specified role (Admin, Student, Instructor, NonMuslim). Accepts multipart/form-data for image upload.")
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status200OK)
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status400BadRequest);

        groupBuilder.MapPost(Login, "login")
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Login user")
            .WithDescription("Authenticates a user and returns a JWT token")
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status200OK)
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status400BadRequest);

        groupBuilder.MapPost(LoginWithGoogle, "login/google")
            .AllowAnonymous()
            .WithName("LoginWithGoogle")
            .WithSummary("Login with Google")
            .WithDescription("Accepts a Google ID token, verifies it and returns a JWT token")
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status200OK)
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status400BadRequest);

        groupBuilder.MapPost(LoginWithFacebook, "login/facebook")
            .AllowAnonymous()
            .WithName("LoginWithFacebook")
            .WithSummary("Login with Facebook")
            .WithDescription("Accepts a Facebook access token, verifies it and returns a JWT token")
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status200OK)
            .Produces<Result<AuthenticationResponse>>(StatusCodes.Status400BadRequest);

        groupBuilder.MapPost(ForgotPassword, "forgot-password")
            .AllowAnonymous()
            .WithName("ForgotPassword")
            .WithSummary("Request password reset OTP")
            .WithDescription("Sends a 6-digit OTP to the user's email for password reset. OTP expires in 10 minutes.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest);

        groupBuilder.MapPost(ResetPassword, "reset-password")
            .AllowAnonymous()
            .WithName("ResetPassword")
            .WithSummary("Reset password with OTP")
            .WithDescription("Validates the OTP and resets the user's password")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest);
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

    public async Task<IResult> ForgotPassword(
        ISender sender,
        ForgotPasswordCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> ResetPassword(
        ISender sender,
        ResetPasswordCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
