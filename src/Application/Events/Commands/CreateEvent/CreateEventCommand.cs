using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
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
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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
            IsPublished = request.IsPublished
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
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
