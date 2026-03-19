using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByUser;

public record GetEventRegistrationsByUserQuery(Guid UserId) : IRequest<Result<List<EventRegistrationDto>>>;

public class GetEventRegistrationsByUserQueryHandler : IRequestHandler<GetEventRegistrationsByUserQuery, Result<List<EventRegistrationDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEventRegistrationsByUserQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<EventRegistrationDto>>> Handle(GetEventRegistrationsByUserQuery request, CancellationToken cancellationToken)
    {
        var registrations = await _context.EventRegistrations
            .Where(r => r.UserId == request.UserId)
            .Select(s => new EventRegistrationDto {
            Id = s.Id,
            EventId = s.EventId,
            UserId = s.UserId,
            FirstName = s.User != null ? s.User.FirstName : string.Empty,
            LastName = s.User != null ? s.User.LastName : string.Empty,
            QuestionText = s.QuestionText,
            Status = s.Status
    })
            .ToListAsync(cancellationToken);
            
        return Result<List<EventRegistrationDto>>.Success(registrations);
    }
}
