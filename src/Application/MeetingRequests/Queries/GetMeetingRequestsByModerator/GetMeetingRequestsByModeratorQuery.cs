using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByModerator;

public record GetMeetingRequestsByModeratorQuery(Guid ModeratorId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<MeetingRequestDto>>>;

public class GetMeetingRequestsByModeratorQueryHandler : IRequestHandler<GetMeetingRequestsByModeratorQuery, Result<List<MeetingRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMeetingRequestsByModeratorQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<MeetingRequestDto>>> Handle(GetMeetingRequestsByModeratorQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MeetingRequests
            .Include(x => x.Requester)
            .AsQueryable()
            .Where(m => m.ModeratorId == request.ModeratorId);
        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }
        var data = await query
            .ProjectTo<MeetingRequestDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return Result<List<MeetingRequestDto>>.Success(data);
    }
}
