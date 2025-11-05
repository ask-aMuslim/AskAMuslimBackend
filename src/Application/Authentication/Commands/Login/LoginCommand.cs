using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.Login;

public record LoginCommand : IRequest<AuthenticationResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthenticationResponse>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthenticationResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginUserAsync(request.Email, request.Password);

        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException(string.Join(", ", result.Errors));
        }

        return result.Data!;
    }
}
