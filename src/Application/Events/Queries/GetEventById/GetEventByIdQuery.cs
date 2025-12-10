using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Events.Queries.GetEventById;

public record GetEventByIdQuery(Guid Id) : IRequest<Result<EventDto>>;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Result<EventDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var eventDto = await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == request.Id)
            .ProjectTo<EventDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (eventDto == null)
        {
            return Result<EventDto>.Failure(new[] { "Event not found" });
        }
        
        return Result<EventDto>.Success(eventDto);
    }
}
