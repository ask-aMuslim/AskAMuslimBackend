using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByEvent;

public record GetEventRegistrationsByEventQuery(Guid EventId) : IRequest<Result<List<EventRegistrationDto>>>;

public class GetEventRegistrationsByEventQueryHandler : IRequestHandler<GetEventRegistrationsByEventQuery, Result<List<EventRegistrationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventRegistrationsByEventQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<EventRegistrationDto>>> Handle(GetEventRegistrationsByEventQuery request, CancellationToken cancellationToken)
    {
        var registrations = await _context.EventRegistrations
            .Include(r => r.User)
            .Where(r => r.EventId == request.EventId)
            .ProjectTo<EventRegistrationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            
        return Result<List<EventRegistrationDto>>.Success(registrations);
    }
}
