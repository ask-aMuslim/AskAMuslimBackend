using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.LoginWithGoogle;

public record LoginWithGoogleCommand(string IdToken) : IRequest<Result<AuthenticationResponse>>;

public class LoginWithGoogleCommandHandler : IRequestHandler<LoginWithGoogleCommand, Result<AuthenticationResponse>>
{
    private readonly IIdentityService _identityService;

    public LoginWithGoogleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(LoginWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginWithGoogleAsync(request.IdToken);
        return result;
    }
}
