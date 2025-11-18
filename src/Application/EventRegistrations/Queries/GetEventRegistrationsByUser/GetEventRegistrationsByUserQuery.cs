using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByUser;

public record GetEventRegistrationsByUserQuery(Guid UserId) : IRequest<List<EventRegistrationDto>>;

public class GetEventRegistrationsByUserQueryHandler : IRequestHandler<GetEventRegistrationsByUserQuery, List<EventRegistrationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventRegistrationsByUserQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EventRegistrationDto>> Handle(GetEventRegistrationsByUserQuery request, CancellationToken cancellationToken)
    {
        return await _context.EventRegistrations
            .Where(r => r.UserId == request.UserId)
            .ProjectTo<EventRegistrationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
