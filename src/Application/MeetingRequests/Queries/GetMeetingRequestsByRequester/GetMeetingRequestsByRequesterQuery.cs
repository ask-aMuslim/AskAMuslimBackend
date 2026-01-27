using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByRequester;

public record GetMeetingRequestsByRequesterQuery(Guid RequesterId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<MeetingRequestDto>>>;

public class GetMeetingRequestsByRequesterQueryHandler : IRequestHandler<GetMeetingRequestsByRequesterQuery, Result<List<MeetingRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMeetingRequestsByRequesterQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<MeetingRequestDto>>> Handle(GetMeetingRequestsByRequesterQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MeetingRequests
            .Include(x => x.Requester)
            .AsQueryable()
            .Where(m => m.RequesterId == request.RequesterId);
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
