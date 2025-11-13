using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByEvent;

public record GetEventRegistrationsByEventQuery(Guid EventId) : IRequest<List<EventRegistrationDto>>;

public class GetEventRegistrationsByEventQueryHandler : IRequestHandler<GetEventRegistrationsByEventQuery, List<EventRegistrationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventRegistrationsByEventQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EventRegistrationDto>> Handle(GetEventRegistrationsByEventQuery request, CancellationToken cancellationToken)
    {
        return await _context.EventRegistrations
            .Where(r => r.EventId == request.EventId)
            .ProjectTo<EventRegistrationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
