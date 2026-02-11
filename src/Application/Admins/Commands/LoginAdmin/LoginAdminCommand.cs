using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Admins.Commands.LoginAdmin;

public record LoginAdminCommand(string Email, string Password) : IRequest<Result<AuthenticationResponse>>;

public class LoginAdminCommandHandler : IRequestHandler<LoginAdminCommand, Result<AuthenticationResponse>>
{
    private readonly IIdentityService _identityService;

    public LoginAdminCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginModeratorAsync(request.Email, request.Password);
        return result;
    }
}
