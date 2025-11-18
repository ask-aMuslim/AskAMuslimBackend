using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByScholar;

public record GetInquiryRequestsByScholarQuery(Guid ScholarId, MeetingInquiryRequestStatus? Status = null) : IRequest<List<InquiryRequestDto>>;

public class GetInquiryRequestsByScholarQueryHandler : IRequestHandler<GetInquiryRequestsByScholarQuery, List<InquiryRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetInquiryRequestsByScholarQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<InquiryRequestDto>> Handle(GetInquiryRequestsByScholarQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InquiryRequests.AsQueryable().Where(i => i.ScholarId == request.ScholarId);
        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }
        return await query.ProjectTo<InquiryRequestDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);
    }
}
