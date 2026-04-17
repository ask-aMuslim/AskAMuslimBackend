using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.CreateEventRegistration;

public record CreateEventRegistrationCommand : IRequest<Result<Guid>>
{
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string? QuestionText { get; init; }
}

public class CreateEventRegistrationCommandHandler : IRequestHandler<CreateEventRegistrationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public CreateEventRegistrationCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);

        var errors = new List<string>();
        if (!eventExists) errors.Add(_localizer["Events.NotFound"]);
        if (!userExists) errors.Add(_localizer["Auth.NotAuthenticated"]);
        if (errors.Any()) return Result<Guid>.Failure(errors);

        var alreadyRegistered = await _context.EventRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.EventId == request.EventId && r.UserId == request.UserId, cancellationToken);
        if (alreadyRegistered)
        {
            return Result<Guid>.Failure(new[] { _localizer["EventRegistrations.AlreadyRegistered"] });
        }

        var entity = new EventRegistration
        {
            EventId = request.EventId,
            UserId = request.UserId,
            QuestionText = request.QuestionText,
            Status = EventRegistrationStatus.Pending
        };
        _context.EventRegistrations.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
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
