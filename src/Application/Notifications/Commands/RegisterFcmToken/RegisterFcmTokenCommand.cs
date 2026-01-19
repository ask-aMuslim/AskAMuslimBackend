using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.Notifications.Commands.RegisterFcmToken;

public record RegisterFcmTokenCommand : IRequest<Result>
{
    public required string FcmToken { get; init; }
}

public class RegisterFcmTokenCommandValidator : AbstractValidator<RegisterFcmTokenCommand>
{
    public RegisterFcmTokenCommandValidator()
    {
        RuleFor(x => x.FcmToken)
            .NotEmpty().WithMessage("FCM token is required")
            .MaximumLength(255).WithMessage("FCM token must not exceed 255 characters");
    }
}

public class RegisterFcmTokenCommandHandler : IRequestHandler<RegisterFcmTokenCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ILogger<RegisterFcmTokenCommandHandler> _logger;

    public RegisterFcmTokenCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        ILogger<RegisterFcmTokenCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(RegisterFcmTokenCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUser.Id;
        if (string.IsNullOrEmpty(userIdString))
        {
            return Result.Failure(new[] { "User not authenticated" });
        }

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Result.Failure(new[] { "Invalid user ID" });
        }

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);

        if (user == null)
        {
            return Result.Failure(new[] { "User not found" });
        }

        user.FcmToken = request.FcmToken;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("FCM token registered for user {UserId}", userId);

        return Result.Success();
    }
}
