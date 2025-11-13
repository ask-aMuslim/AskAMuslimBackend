using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.UpdateEventRegistrationStatus;

public record UpdateEventRegistrationStatusCommand : IRequest
{
    public Guid Id { get; init; }
    public EventStatus Status { get; init; }
}

public class UpdateEventRegistrationStatusCommandHandler : IRequestHandler<UpdateEventRegistrationStatusCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateEventRegistrationStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateEventRegistrationStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Id", "Registration not found.") });
        }

        entity.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateEventRegistrationStatusCommandValidator : AbstractValidator<UpdateEventRegistrationStatusCommand>
{
    public UpdateEventRegistrationStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
