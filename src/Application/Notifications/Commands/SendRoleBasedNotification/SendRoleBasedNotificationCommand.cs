using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Notifications.Commands.SendRoleBasedNotification;

public record SendRoleBasedNotificationCommand : IRequest<Result<string>>
{
    public required UserRole Role { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ActionUrl { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, string>? CustomData { get; init; }
}

public class SendRoleBasedNotificationCommandValidator : AbstractValidator<SendRoleBasedNotificationCommand>
{
    public SendRoleBasedNotificationCommandValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role specified");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(1000).WithMessage("Message must not exceed 1000 characters");
    }
}

public class SendRoleBasedNotificationCommandHandler : IRequestHandler<SendRoleBasedNotificationCommand, Result<string>>
{
    private readonly IBackgroundJobService _backgroundJobService;

    public SendRoleBasedNotificationCommandHandler(IBackgroundJobService backgroundJobService)
    {
        _backgroundJobService = backgroundJobService;
    }

    public Task<Result<string>> Handle(SendRoleBasedNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new PushNotificationRequest
        {
            Title = request.Title,
            Body = request.Message,
            ActionUrl = request.ActionUrl,
            ImageUrl = request.ImageUrl,
            CustomData = request.CustomData
        };

        var jobId = _backgroundJobService.EnqueueRoleBasedNotification(request.Role, notification);

        return Task.FromResult(Result<string>.Success(jobId));
    }
}
