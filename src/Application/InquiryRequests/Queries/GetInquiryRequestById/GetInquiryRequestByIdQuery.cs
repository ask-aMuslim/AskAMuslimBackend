using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestById;

public record GetInquiryRequestByIdQuery(Guid Id) : IRequest<InquiryRequestDto?>;

public class GetInquiryRequestByIdQueryHandler : IRequestHandler<GetInquiryRequestByIdQuery, InquiryRequestDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetInquiryRequestByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<InquiryRequestDto?> Handle(GetInquiryRequestByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.InquiryRequests
            .Where(i => i.Id == request.Id)
            .ProjectTo<InquiryRequestDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
