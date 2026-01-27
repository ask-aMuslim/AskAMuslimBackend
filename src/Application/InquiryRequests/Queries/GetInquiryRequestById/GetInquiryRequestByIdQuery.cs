using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestById;

public record GetInquiryRequestByIdQuery(Guid Id) : IRequest<Result<InquiryRequestDto?>>;

public class GetInquiryRequestByIdQueryHandler : IRequestHandler<GetInquiryRequestByIdQuery, Result<InquiryRequestDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetInquiryRequestByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<InquiryRequestDto?>> Handle(GetInquiryRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var data = await _context.InquiryRequests
            .Include(x => x.Requester)
            .Where(i => i.Id == request.Id)
            .ProjectTo<InquiryRequestDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        return Result<InquiryRequestDto?>.Success(data);
    }
}
