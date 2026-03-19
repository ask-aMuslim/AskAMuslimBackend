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
    public bool? IsAssigned { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetAllInquiryRequestsQueryHandler : IRequestHandler<GetAllInquiryRequestsQuery, Result<PaginatedList<InquiryRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllInquiryRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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

        if (request.IsAssigned.HasValue)
        {
            query = request.IsAssigned.Value
                ? query.Where(ir => ir.ModeratorId.HasValue)
                : query.Where(ir => !ir.ModeratorId.HasValue);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(ir => ir.Message.Contains(request.SearchTerm));
        }

        var paginatedList = await query
            .OrderByDescending(ir => ir.Created)
            .Select(s => new InquiryRequestDto {
            Id = s.Id,
            RequesterId = s.RequesterId,
            RequesterName = s.Requester != null ? s.Requester.FirstName + " " + s.Requester.LastName : string.Empty,
            ModeratorName = s.Moderator != null ? s.Moderator.FirstName + " " + s.Moderator.LastName : null,
            ModeratorId = s.ModeratorId,
            Topic = s.Topic,
            Message = s.Message,
            Languages = s.Languages,
            Status = s.Status,
            Response = s.Response,
            Created = s.Created
    })
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
