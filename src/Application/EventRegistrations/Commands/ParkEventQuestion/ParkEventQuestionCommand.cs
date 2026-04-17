using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.ParkEventQuestion;

public record ParkEventQuestionCommand : IRequest<Result>
{
    public Guid EventRegistrationId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
}

public class ParkEventQuestionCommandHandler : IRequestHandler<ParkEventQuestionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ILocalizationService _localizer;

    public ParkEventQuestionCommandHandler(IApplicationDbContext context, IUser currentUser, ILocalizationService localizer)
    {
        _context = context;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    public async Task<Result> Handle(ParkEventQuestionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result.Failure(new[] { _localizer["Auth.NotAuthenticated"] });
        }

        var registration = await _context.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == request.EventRegistrationId && r.UserId == userId, cancellationToken);

        if (registration is null)
        {
            return Result.Failure(new[] { _localizer["EventRegistrations.NotFound"] });
        }

        registration.QuestionText = request.QuestionText;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class ParkEventQuestionCommandValidator : AbstractValidator<ParkEventQuestionCommand>
{
    public ParkEventQuestionCommandValidator()
    {
        RuleFor(x => x.EventRegistrationId).NotEmpty();
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
    }
}
