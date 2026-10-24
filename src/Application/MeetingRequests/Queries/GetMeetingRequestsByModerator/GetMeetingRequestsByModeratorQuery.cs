using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.MeetingRequests.Common;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByModerator;

public record GetMeetingRequestsByModeratorQuery(Guid ModeratorId, MeetingInquiryRequestStatus? Status = null) : IRequest<Result<List<MeetingRequestDto>>>;

public class GetMeetingRequestsByModeratorQueryHandler : IRequestHandler<GetMeetingRequestsByModeratorQuery, Result<List<MeetingRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IPermissionService _permissionService;

    public GetMeetingRequestsByModeratorQueryHandler(IApplicationDbContext context, IUser user, IPermissionService permissionService)
    {
        _context = context;
        _user = user;
        _permissionService = permissionService;
    }

    public async Task<Result<List<MeetingRequestDto>>> Handle(GetMeetingRequestsByModeratorQuery request, CancellationToken cancellationToken)
    {
        await MeetingRequestAccess.EnsureCanViewListOfAsync(request.ModeratorId, _user, _permissionService, cancellationToken);

        var query = _context.MeetingRequests
            .Include(x => x.Requester)
            .AsQueryable()
            .Where(m => m.ModeratorId == request.ModeratorId);
        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }
        var data = await query
            .Select(s => new MeetingRequestDto {
            Id = s.Id,
            RequesterId = s.RequesterId,
            RequesterName = s.Requester != null ? s.Requester.FirstName + " " + s.Requester.LastName : string.Empty,
            RequesterEmail = s.Requester != null ? s.Requester.Email : null,
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
