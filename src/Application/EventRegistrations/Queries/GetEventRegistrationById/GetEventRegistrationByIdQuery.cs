using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationById;

public record GetEventRegistrationByIdQuery(Guid Id) : IRequest<EventRegistrationDto?>;

public class GetEventRegistrationByIdQueryHandler : IRequestHandler<GetEventRegistrationByIdQuery, EventRegistrationDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventRegistrationByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<EventRegistrationDto?> Handle(GetEventRegistrationByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.EventRegistrations
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .ProjectTo<EventRegistrationDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
