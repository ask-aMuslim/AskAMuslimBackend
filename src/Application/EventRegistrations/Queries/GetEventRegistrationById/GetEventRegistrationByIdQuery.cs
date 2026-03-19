using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationById;

public record GetEventRegistrationByIdQuery(Guid Id) : IRequest<Result<EventRegistrationDto?>>;

public class GetEventRegistrationByIdQueryHandler : IRequestHandler<GetEventRegistrationByIdQuery, Result<EventRegistrationDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetEventRegistrationByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<EventRegistrationDto?>> Handle(GetEventRegistrationByIdQuery request, CancellationToken cancellationToken)
    {
        var registration = await _context.EventRegistrations
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .Select(s => new EventRegistrationDto {
            Id = s.Id,
            EventId = s.EventId,
            UserId = s.UserId,
            FirstName = s.User != null ? s.User.FirstName : string.Empty,
            LastName = s.User != null ? s.User.LastName : string.Empty,
            QuestionText = s.QuestionText,
            Status = s.Status
    })
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<EventRegistrationDto?>.Success(registration);
    }
}
