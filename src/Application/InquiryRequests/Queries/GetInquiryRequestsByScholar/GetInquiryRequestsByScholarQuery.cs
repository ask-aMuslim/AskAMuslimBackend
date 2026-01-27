using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByScholar;

public record GetInquiryRequestsByScholarQuery(Guid ScholarId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<InquiryRequestDto>>>;

public class GetInquiryRequestsByScholarQueryHandler : IRequestHandler<GetInquiryRequestsByScholarQuery, Result<List<InquiryRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetInquiryRequestsByScholarQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<InquiryRequestDto>>> Handle(GetInquiryRequestsByScholarQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InquiryRequests
            .Include(x => x.Requester)
            .AsQueryable()
            .Where(i => i.ScholarId == request.ScholarId);
        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }
        var data = await query.ProjectTo<InquiryRequestDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);
        return Result<List<InquiryRequestDto>>.Success(data);
    }
}
