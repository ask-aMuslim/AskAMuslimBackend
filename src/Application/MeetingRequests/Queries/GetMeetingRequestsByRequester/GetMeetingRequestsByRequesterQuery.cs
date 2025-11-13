using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByRequester;

public record GetMeetingRequestsByRequesterQuery(Guid RequesterId, MeetingRequestStatus? Status = null) : IRequest<List<MeetingRequestDto>>;

public class GetMeetingRequestsByRequesterQueryHandler : IRequestHandler<GetMeetingRequestsByRequesterQuery, List<MeetingRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMeetingRequestsByRequesterQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<MeetingRequestDto>> Handle(GetMeetingRequestsByRequesterQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MeetingRequests.AsQueryable().Where(m => m.RequesterId == request.RequesterId);
        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }
        return await query
            .ProjectTo<MeetingRequestDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
