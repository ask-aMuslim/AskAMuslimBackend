using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.UpdateEventRegistrationStatus;

public record UpdateEventRegistrationStatusCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public EventStatus Status { get; init; }
}

public class UpdateEventRegistrationStatusCommandHandler : IRequestHandler<UpdateEventRegistrationStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateEventRegistrationStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateEventRegistrationStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(new[] { "Registration not found." });
        }

        entity.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
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
