using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.LoginWithFacebook;

public record LoginWithFacebookCommand(string AccessToken) : IRequest<AuthenticationResponse>;

public class LoginWithFacebookCommandHandler : IRequestHandler<LoginWithFacebookCommand, AuthenticationResponse>
{
    private readonly IIdentityService _identityService;

    public LoginWithFacebookCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthenticationResponse> Handle(LoginWithFacebookCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginWithFacebookAsync(request.AccessToken);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException(string.Join(", ", result.Errors));
        }
        return result.Data!;
    }
}
