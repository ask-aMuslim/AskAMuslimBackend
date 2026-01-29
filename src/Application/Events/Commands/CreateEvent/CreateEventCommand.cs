using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Events.Commands.CreateEvent;

public record CreateEventCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? SpeakerName { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public string? MeetingLink { get; init; }
    public bool IsPublished { get; init; }
    
    /// <summary>
    /// Target role for this event. If null, event is for all users (general event).
    /// </summary>
    public UserRole? TargetRole { get; init; }
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEventNotificationService _notificationService;

    public CreateEventCommandHandler(
        IApplicationDbContext context,
        IEventNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = new Event
        {
            Title = request.Title,
            Description = request.Description,
            SpeakerName = request.SpeakerName,
            ImageUrl = request.ImageUrl,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            MeetingLink = request.MeetingLink,
            IsPublished = request.IsPublished,
            TargetRole = request.TargetRole
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Send email notifications to target users (only if published)
        if (request.IsPublished)
        {
            // Fire and forget - don't wait for email sending
            _ = Task.Run(async () =>
            {
                await _notificationService.SendEventNotificationAsync(
                    entity.Id,
                    request.Title,
                    request.Description,
                    request.StartDateTime,
                    request.EndDateTime,
                    request.MeetingLink,
                    request.SpeakerName,
                    request.TargetRole,
                    CancellationToken.None);
            }, CancellationToken.None);
        }

        return Result<Guid>.Success(entity.Id);
    }
}

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.SpeakerName).MaximumLength(200);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.MeetingLink).MaximumLength(500);
        RuleFor(x => x.StartDateTime).NotEmpty();
        RuleFor(x => x.EndDateTime)
            .NotEmpty()
            .Must((cmd, end) => end >= cmd.StartDateTime).WithMessage("EndDateTime must be after StartDateTime.");
    }
}
