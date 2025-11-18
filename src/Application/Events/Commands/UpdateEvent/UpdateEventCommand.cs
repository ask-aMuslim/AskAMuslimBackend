using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Events.Commands.UpdateEvent;

public record UpdateEventCommand : IRequest
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? SpeakerName { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public string? MeetingLink { get; init; }
    public bool IsPublished { get; init; }
}

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Events.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Id", "Event not found.") });
        }

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.SpeakerName = request.SpeakerName;
        entity.ImageUrl = request.ImageUrl;
        entity.StartDateTime = request.StartDateTime;
        entity.EndDateTime = request.EndDateTime;
        entity.MeetingLink = request.MeetingLink;
        entity.IsPublished = request.IsPublished;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
