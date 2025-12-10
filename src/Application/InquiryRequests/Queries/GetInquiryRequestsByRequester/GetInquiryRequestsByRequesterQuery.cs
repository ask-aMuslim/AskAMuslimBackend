using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByRequester;

public record GetInquiryRequestsByRequesterQuery(Guid RequesterId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<InquiryRequestDto>>>;

public class GetInquiryRequestsByRequesterQueryHandler : IRequestHandler<GetInquiryRequestsByRequesterQuery, Result<List<InquiryRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetInquiryRequestsByRequesterQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<InquiryRequestDto>>> Handle(GetInquiryRequestsByRequesterQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InquiryRequests.AsQueryable().Where(i => i.RequesterId == request.RequesterId);
        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }
        var data = await query.ProjectTo<InquiryRequestDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);
        return Result<List<InquiryRequestDto>>.Success(data);
    }
}
