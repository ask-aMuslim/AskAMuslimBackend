using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByScholar;

public record GetMeetingRequestsByScholarQuery(Guid ScholarId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<MeetingRequestDto>>>;

public class GetMeetingRequestsByScholarQueryHandler : IRequestHandler<GetMeetingRequestsByScholarQuery, Result<List<MeetingRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetMeetingRequestsByScholarQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<MeetingRequestDto>>> Handle(GetMeetingRequestsByScholarQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MeetingRequests.AsQueryable().Where(m => m.ScholarId == request.ScholarId);
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
