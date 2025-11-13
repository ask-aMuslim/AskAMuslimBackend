using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.CreateEventRegistration;

public record CreateEventRegistrationCommand : IRequest<Guid>
{
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string? QuestionText { get; init; }
}

public class CreateEventRegistrationCommandHandler : IRequestHandler<CreateEventRegistrationCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateEventRegistrationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!eventExists || !userExists)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            if (!eventExists) failures.Add(new("EventId", "Event does not exist."));
            if (!userExists) failures.Add(new("UserId", "User does not exist."));
            throw new ValidationException(failures);
        }

        var alreadyRegistered = await _context.EventRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.EventId == request.EventId && r.UserId == request.UserId, cancellationToken);
        if (alreadyRegistered)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("UserId", "User is already registered for this event.") });
        }

        var entity = new EventRegistration
        {
            EventId = request.EventId,
            UserId = request.UserId,
            QuestionText = request.QuestionText,
            Status = EventStatus.Pending
        };
        _context.EventRegistrations.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public class CreateEventRegistrationCommandValidator : AbstractValidator<CreateEventRegistrationCommand>
{
    public CreateEventRegistrationCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.QuestionText).MaximumLength(2000);
    }
}
