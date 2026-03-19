using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByRequester;

public record GetMeetingRequestsByRequesterQuery(Guid RequesterId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<MeetingRequestDto>>>;

public class GetMeetingRequestsByRequesterQueryHandler : IRequestHandler<GetMeetingRequestsByRequesterQuery, Result<List<MeetingRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetMeetingRequestsByRequesterQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<MeetingRequestDto>>> Handle(GetMeetingRequestsByRequesterQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MeetingRequests
            .Include(x => x.Requester)
            .AsQueryable()
            .Where(m => m.RequesterId == request.RequesterId);
        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }
        var data = await query
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
            .ToListAsync(cancellationToken);
        return Result<List<MeetingRequestDto>>.Success(data);
    }
}
