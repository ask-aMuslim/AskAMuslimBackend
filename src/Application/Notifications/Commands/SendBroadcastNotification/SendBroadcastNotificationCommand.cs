using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.Notifications.Commands.SendBroadcastNotification;

public record SendBroadcastNotificationCommand : IRequest<Result<string>>
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ActionUrl { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, string>? CustomData { get; init; }
}

public class SendBroadcastNotificationCommandValidator : AbstractValidator<SendBroadcastNotificationCommand>
{
    public SendBroadcastNotificationCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(1000).WithMessage("Message must not exceed 1000 characters");
    }
}

public class SendBroadcastNotificationCommandHandler : IRequestHandler<SendBroadcastNotificationCommand, Result<string>>
{
    private readonly IBackgroundJobService _backgroundJobService;

    public SendBroadcastNotificationCommandHandler(IBackgroundJobService backgroundJobService)
    {
        _backgroundJobService = backgroundJobService;
    }

    public Task<Result<string>> Handle(SendBroadcastNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new PushNotificationRequest
        {
            Title = request.Title,
            Body = request.Message,
            ActionUrl = request.ActionUrl,
            ImageUrl = request.ImageUrl,
            CustomData = request.CustomData
        };

        var jobId = _backgroundJobService.EnqueueBroadcastNotification(notification);

        return Task.FromResult(Result<string>.Success(jobId));
    }
}
