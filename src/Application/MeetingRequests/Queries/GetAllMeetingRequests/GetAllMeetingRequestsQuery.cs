using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetAllMeetingRequests;

public record GetAllMeetingRequestsQuery : IRequest<Result<PaginatedList<MeetingRequestDto>>>, IPaginationQuery
{
    public MeetingInquiryRequestStatus? Status { get; init; }
    public MeetingInquiryTopic? Topic { get; init; }
    public bool? IsAssigned { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetAllMeetingRequestsQueryHandler : IRequestHandler<GetAllMeetingRequestsQuery, Result<PaginatedList<MeetingRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllMeetingRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<MeetingRequestDto>>> Handle(GetAllMeetingRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MeetingRequests
            .Include(x => x.Requester)
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(mr => mr.Status == request.Status.Value);
        }

        if (request.Topic.HasValue)
        {
            query = query.Where(mr => mr.Topic == request.Topic.Value);
        }

        if (request.IsAssigned.HasValue)
        {
            query = request.IsAssigned.Value
                ? query.Where(mr => mr.ModeratorId.HasValue)
                : query.Where(mr => !mr.ModeratorId.HasValue);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(mr => mr.Message.ToLower().Contains(searchLower));
        }

        var paginatedList = await query
            .OrderByDescending(mr => mr.Created)
            .Select(s => new MeetingRequestDto {
            Id = s.Id,
            RequesterId = s.RequesterId,
            RequesterName = s.Requester != null ? s.Requester.FirstName + " " + s.Requester.LastName : string.Empty,
            ModeratorName = s.Moderator != null ? s.Moderator.FirstName + " " + s.Moderator.LastName : null,
            ModeratorId = s.ModeratorId,
            Topic = s.Topic,
            Message = s.Message,
            Languages = s.Languages,
            ScheduledAt = s.ScheduledAt,
            Status = s.Status,
            Response = s.Response,
            CreatedAt = s.Created
    })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<MeetingRequestDto>>.Success(paginatedList);
    }
}

public class GetAllMeetingRequestsQueryValidator : PaginationQueryValidator<GetAllMeetingRequestsQuery>
{
    public GetAllMeetingRequestsQueryValidator() : base()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Topic)
            .IsInEnum()
            .When(x => x.Topic.HasValue);
    }
}
