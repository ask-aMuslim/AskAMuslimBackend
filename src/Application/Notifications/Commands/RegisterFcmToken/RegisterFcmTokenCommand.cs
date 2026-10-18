using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.Notifications.Commands.RegisterFcmToken;

/// <summary>
/// Registers the current device's FCM token for the current user. A user can have several devices;
/// calling this again with the same token just refreshes it.
/// </summary>
public record RegisterFcmTokenCommand : IRequest<Result>
{
    public required string FcmToken { get; init; }
    public DevicePlatform Platform { get; init; } = DevicePlatform.Unknown;
}

public class RegisterFcmTokenCommandValidator : AbstractValidator<RegisterFcmTokenCommand>
{
    public RegisterFcmTokenCommandValidator()
    {
        RuleFor(x => x.FcmToken)
            .NotEmpty().WithMessage("FCM token is required")
            .MaximumLength(255).WithMessage("FCM token must not exceed 255 characters");

        RuleFor(x => x.Platform).IsInEnum();
    }
}

public class RegisterFcmTokenCommandHandler : IRequestHandler<RegisterFcmTokenCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RegisterFcmTokenCommandHandler> _logger;

    public RegisterFcmTokenCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider,
        ILogger<RegisterFcmTokenCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
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

        var now = _timeProvider.GetUtcNow();

        // The same device may have been registered by another account (log out / log in as someone else),
        // so the token moves to the current user.
        var deviceToken = await _context.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.Token == request.FcmToken, cancellationToken);

        if (deviceToken == null)
        {
            _context.UserDeviceTokens.Add(new UserDeviceToken
            {
                UserId = userId,
                Token = request.FcmToken,
                Platform = request.Platform,
                LastSeenAt = now
            });
        }
        else
        {
            deviceToken.UserId = userId;
            deviceToken.LastSeenAt = now;
            if (request.Platform != DevicePlatform.Unknown)
            {
                deviceToken.Platform = request.Platform;
            }
        }

        // Kept for older code paths that still read a single token per user.
        user.FcmToken = request.FcmToken;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("FCM token registered for user {UserId} ({Platform})", userId, request.Platform);

        return Result.Success();
    }
}
