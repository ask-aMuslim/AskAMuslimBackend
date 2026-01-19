using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.Notifications.Commands.SendBulkNotifications;

public record SendBulkNotificationsCommand : IRequest<Result<string>>
{
    public required IEnumerable<Guid> UserIds { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ActionUrl { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, string>? CustomData { get; init; }
}

public class SendBulkNotificationsCommandValidator : AbstractValidator<SendBulkNotificationsCommand>
{
    public SendBulkNotificationsCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(1000).WithMessage("Message must not exceed 1000 characters");
    }
}

public class SendBulkNotificationsCommandHandler : IRequestHandler<SendBulkNotificationsCommand, Result<string>>
{
    private readonly IBackgroundJobService _backgroundJobService;

    public SendBulkNotificationsCommandHandler(IBackgroundJobService backgroundJobService)
    {
        _backgroundJobService = backgroundJobService;
    }

    public Task<Result<string>> Handle(SendBulkNotificationsCommand request, CancellationToken cancellationToken)
    {
        var notification = new PushNotificationRequest
        {
            Title = request.Title,
            Body = request.Message,
            ActionUrl = request.ActionUrl,
            ImageUrl = request.ImageUrl,
            CustomData = request.CustomData
        };

        var jobId = _backgroundJobService.EnqueueBulkNotification(request.UserIds, notification);

        return Task.FromResult(Result<string>.Success(jobId));
    }
}
