using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestCounts;

public class MeetingRequestCountsDto
{
    /// <summary>Requests still waiting for a scholar to accept or reject them.</summary>
    public int Pending { get; init; }
    /// <summary>Pending or accepted requests that have no moderator yet.</summary>
    public int Unassigned { get; init; }
}

public record GetMeetingRequestCountsQuery : IRequest<Result<MeetingRequestCountsDto>>;

public class GetMeetingRequestCountsQueryHandler : IRequestHandler<GetMeetingRequestCountsQuery, Result<MeetingRequestCountsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMeetingRequestCountsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MeetingRequestCountsDto>> Handle(GetMeetingRequestCountsQuery request, CancellationToken cancellationToken)
    {
        var requests = _context.MeetingRequests.AsNoTracking();

        var counts = new MeetingRequestCountsDto
        {
            Pending = await requests.CountAsync(m => m.Status == MeetingInquiryRequestStatus.Pending, cancellationToken),
            Unassigned = await requests.CountAsync(
                m => m.ModeratorId == null &&
                     (m.Status == MeetingInquiryRequestStatus.Pending || m.Status == MeetingInquiryRequestStatus.Accepted),
                cancellationToken)
        };

        return Result<MeetingRequestCountsDto>.Success(counts);
    }
}
