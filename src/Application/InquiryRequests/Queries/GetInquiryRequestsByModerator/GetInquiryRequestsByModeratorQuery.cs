using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByModerator;

public record GetInquiryRequestsByModeratorQuery(Guid ModeratorId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<InquiryRequestDto>>>;

public class GetInquiryRequestsByModeratorQueryHandler : IRequestHandler<GetInquiryRequestsByModeratorQuery, Result<List<InquiryRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetInquiryRequestsByModeratorQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<InquiryRequestDto>>> Handle(GetInquiryRequestsByModeratorQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InquiryRequests
            .Include(x => x.Requester)
            .AsQueryable()
            .Where(i => i.ModeratorId == request.ModeratorId);
        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }
        var data = await query.ProjectTo<InquiryRequestDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);
        return Result<List<InquiryRequestDto>>.Success(data);
    }
}
