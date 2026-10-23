using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries.GetMyMeetingRequestById;

/// <summary>One of the signed-in user's own meeting requests. Anyone else's request is reported as not found.</summary>
public record GetMyMeetingRequestByIdQuery(Guid Id) : IRequest<Result<MeetingRequestDto>>;

public class GetMyMeetingRequestByIdQueryHandler : IRequestHandler<GetMyMeetingRequestByIdQuery, Result<MeetingRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMyMeetingRequestByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<MeetingRequestDto>> Handle(GetMyMeetingRequestByIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<MeetingRequestDto>.Failure(new[] { "Unauthorized" });
        }

        var userId = Guid.Parse(_user.Id);

        var data = await _context.MeetingRequests
            .Where(m => m.Id == request.Id && m.RequesterId == userId)
            .Select(s => new MeetingRequestDto
            {
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

        return data == null
            ? Result<MeetingRequestDto>.Failure(new[] { "Meeting request not found" })
            : Result<MeetingRequestDto>.Success(data);
    }
}
