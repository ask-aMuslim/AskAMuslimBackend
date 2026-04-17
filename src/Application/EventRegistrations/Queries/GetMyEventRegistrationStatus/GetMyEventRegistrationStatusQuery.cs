using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetMyEventRegistrationStatus;

public record GetMyEventRegistrationStatusQuery(Guid EventId) : IRequest<Result<MyEventRegistrationStatusDto>>;

public class MyEventRegistrationStatusDto
{
    public bool IsRegistered { get; init; }
    public string? QuestionText { get; init; }
    public EventRegistrationStatus? EventRegistrationStatus { get; init; }
}

public class GetMyEventRegistrationStatusQueryHandler : IRequestHandler<GetMyEventRegistrationStatusQuery, Result<MyEventRegistrationStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ILocalizationService _localizer;

    public GetMyEventRegistrationStatusQueryHandler(IApplicationDbContext context, IUser currentUser, ILocalizationService localizer)
    {
        _context = context;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    public async Task<Result<MyEventRegistrationStatusDto>> Handle(GetMyEventRegistrationStatusQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<MyEventRegistrationStatusDto>.Failure(new[] { _localizer["Auth.NotAuthenticated"] });
        }

        var registration = await _context.EventRegistrations
            .AsNoTracking()
            .Where(r => r.EventId == request.EventId && r.UserId == userId)
            .Select(r => new
            {
                r.QuestionText,
                r.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (registration is null)
        {
            return Result<MyEventRegistrationStatusDto>.Success(new MyEventRegistrationStatusDto
            {
                IsRegistered = false
            });
        }

        return Result<MyEventRegistrationStatusDto>.Success(new MyEventRegistrationStatusDto
        {
            IsRegistered = true,
            QuestionText = registration.QuestionText,
            EventRegistrationStatus = registration.Status
        });
    }
}
