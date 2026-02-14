using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.Logout;

public record LogoutCommand : IRequest<Result>
{
    public string TokenId { get; init; } = string.Empty;
    public DateTime TokenExpiration { get; init; }
}

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;

    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenId))
        {
            return Result.Failure(new[] { "Invalid token: missing token ID." });
        }

        return await _identityService.LogoutAsync(request.TokenId, request.TokenExpiration);
    }
}
