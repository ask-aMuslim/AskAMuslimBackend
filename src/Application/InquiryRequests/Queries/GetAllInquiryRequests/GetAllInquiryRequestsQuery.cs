using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries.GetAllInquiryRequests;

public record GetAllInquiryRequestsQuery : IRequest<Result<PaginatedList<InquiryRequestDto>>>, IPaginationQuery
{
    public MeetingInquiryRequestStatus? Status { get; init; }
    public MeetingInquiryTopic? Topic { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetAllInquiryRequestsQueryHandler : IRequestHandler<GetAllInquiryRequestsQuery, Result<PaginatedList<InquiryRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllInquiryRequestsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<InquiryRequestDto>>> Handle(GetAllInquiryRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InquiryRequests
            .Include(x => x.Requester)
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(ir => ir.Status == request.Status.Value);
        }

        if (request.Topic.HasValue)
        {
            query = query.Where(ir => ir.Topic == request.Topic.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(ir => ir.Message.Contains(request.SearchTerm));
        }

        var paginatedList = await query
            .OrderByDescending(ir => ir.Created)
            .ProjectTo<InquiryRequestDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<InquiryRequestDto>>.Success(paginatedList);
    }
}

public class GetAllInquiryRequestsQueryValidator : PaginationQueryValidator<GetAllInquiryRequestsQuery>
{
    public GetAllInquiryRequestsQueryValidator() : base()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Topic)
            .IsInEnum()
            .When(x => x.Topic.HasValue);
    }
}
