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
}

public class CreateEventRegistrationCommandHandler : IRequestHandler<CreateEventRegistrationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ILocalizationService _localizer;

    public CreateEventRegistrationCommandHandler(IApplicationDbContext context, IUser currentUser, ILocalizationService localizer)
    {
        _context = context;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<Guid>.Failure(new[] { _localizer["Auth.NotAuthenticated"] });
        }

        var eventExists = await _context.Events.AnyAsync(e => e.Id == request.EventId, cancellationToken);
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);

        var errors = new List<string>();
        if (!eventExists) errors.Add(_localizer["Events.NotFound"]);
        if (!userExists) errors.Add(_localizer["Auth.NotAuthenticated"]);
        if (errors.Any()) return Result<Guid>.Failure(errors);

        var alreadyRegistered = await _context.EventRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.EventId == request.EventId && r.UserId == userId, cancellationToken);
        if (alreadyRegistered)
        {
            return Result<Guid>.Failure(new[] { _localizer["EventRegistrations.AlreadyRegistered"] });
        }

        var entity = new EventRegistration
        {
            EventId = request.EventId,
            UserId = userId,
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
    }
}
