using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.LoginWithGoogle;

public record LoginWithGoogleCommand(string IdToken) : IRequest<AuthenticationResponse>;

public class LoginWithGoogleCommandHandler : IRequestHandler<LoginWithGoogleCommand, AuthenticationResponse>
{
    private readonly IIdentityService _identityService;

    public LoginWithGoogleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<AuthenticationResponse> Handle(LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginWithGoogleAsync(request.IdToken);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException(string.Join(", ", result.Errors));
        }
        return result.Data!;
    }
}
