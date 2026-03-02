using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.UpdateEventRegistrationStatus;

public record UpdateEventRegistrationStatusCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public EventStatus Status { get; init; }
}

public class UpdateEventRegistrationStatusCommandHandler : IRequestHandler<UpdateEventRegistrationStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public UpdateEventRegistrationStatusCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateEventRegistrationStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(new[] { _localizer["EventRegistrations.NotFound"] });
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
