using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.Notifications.Commands.UnregisterFcmToken;

/// <summary>
/// Removes the current device's FCM token (call on logout) so the device stops receiving the user's pushes.
/// </summary>
public record UnregisterFcmTokenCommand : IRequest<Result>
{
    public required string FcmToken { get; init; }
}

public class UnregisterFcmTokenCommandValidator : AbstractValidator<UnregisterFcmTokenCommand>
{
    public UnregisterFcmTokenCommandValidator()
    {
        RuleFor(x => x.FcmToken).NotEmpty();
    }
}

public class UnregisterFcmTokenCommandHandler : IRequestHandler<UnregisterFcmTokenCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UnregisterFcmTokenCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnregisterFcmTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result.Failure(new[] { "User not authenticated" });
        }

        var deviceTokens = await _context.UserDeviceTokens
            .Where(t => t.UserId == userId && t.Token == request.FcmToken)
            .ToListAsync(cancellationToken);

        _context.UserDeviceTokens.RemoveRange(deviceTokens);

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null && user.FcmToken == request.FcmToken)
        {
            user.FcmToken = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Idempotent: unregistering an unknown token is not an error.
        return Result.Success();
    }
}
