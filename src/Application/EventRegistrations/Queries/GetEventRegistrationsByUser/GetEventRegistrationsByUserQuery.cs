using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationsByUser;

public record GetEventRegistrationsByUserQuery(Guid UserId) : IRequest<Result<List<EventRegistrationDto>>>;

public class GetEventRegistrationsByUserQueryHandler : IRequestHandler<GetEventRegistrationsByUserQuery, Result<List<EventRegistrationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventRegistrationsByUserQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<EventRegistrationDto>>> Handle(GetEventRegistrationsByUserQuery request, CancellationToken cancellationToken)
    {
        var registrations = await _context.EventRegistrations
            .Where(r => r.UserId == request.UserId)
            .ProjectTo<EventRegistrationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            
        return Result<List<EventRegistrationDto>>.Success(registrations);
    }
}
