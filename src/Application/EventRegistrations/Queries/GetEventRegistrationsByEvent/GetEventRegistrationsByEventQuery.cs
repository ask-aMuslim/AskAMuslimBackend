using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByEvent;

public record GetEventRegistrationsByEventQuery(Guid EventId) : IRequest<Result<List<EventRegistrationDto>>>;

public class GetEventRegistrationsByEventQueryHandler : IRequestHandler<GetEventRegistrationsByEventQuery, Result<List<EventRegistrationDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEventRegistrationsByEventQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<EventRegistrationDto>>> Handle(GetEventRegistrationsByEventQuery request, CancellationToken cancellationToken)
    {
        var registrations = await _context.EventRegistrations
            .Include(r => r.User)
            .Where(r => r.EventId == request.EventId)
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
