using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestById;

public record GetMeetingRequestByIdQuery(Guid Id) : IRequest<Result<MeetingRequestDto?>>;

public class GetMeetingRequestByIdQueryHandler : IRequestHandler<GetMeetingRequestByIdQuery, Result<MeetingRequestDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetMeetingRequestByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MeetingRequestDto?>> Handle(GetMeetingRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var data = await _context.MeetingRequests
            .Include(x => x.Requester)
            .Include(x => x.Moderator)
            .Where(m => m.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);
        return Result<MeetingRequestDto?>.Success(data);
    }
}
