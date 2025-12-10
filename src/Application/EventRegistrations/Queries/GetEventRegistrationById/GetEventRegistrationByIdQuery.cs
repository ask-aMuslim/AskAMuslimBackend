using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries.GetEventRegistrationById;

public record GetEventRegistrationByIdQuery(Guid Id) : IRequest<Result<EventRegistrationDto?>>;

public class GetEventRegistrationByIdQueryHandler : IRequestHandler<GetEventRegistrationByIdQuery, Result<EventRegistrationDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventRegistrationByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<EventRegistrationDto?>> Handle(GetEventRegistrationByIdQuery request, CancellationToken cancellationToken)
    {
        var registration = await _context.EventRegistrations
            .AsNoTracking()
            .Where(r => r.Id == request.Id)
            .ProjectTo<EventRegistrationDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<EventRegistrationDto?>.Success(registration);
    }
}
