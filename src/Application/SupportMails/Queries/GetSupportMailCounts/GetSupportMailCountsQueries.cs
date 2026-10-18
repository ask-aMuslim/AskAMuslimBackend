using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailCounts;

public class SupportMailCountsDto
{
    /// <summary>Open mails whose last message is from the user (in scope).</summary>
    public int AwaitingResponse { get; init; }
    /// <summary>Open mails with no assigned moderator (admin scope only; 0 for moderators).</summary>
    public int Unassigned { get; init; }
    /// <summary>Mails assigned to the current user that are awaiting a response.</summary>
    public int AssignedToMeAwaiting { get; init; }
}

/// <summary>
/// Badge counts for the admin dashboard (all mails) or the moderator view (<see cref="AssignedToMe"/>).
/// </summary>
public record GetSupportMailCountsQuery(bool AssignedToMe = false) : IRequest<Result<SupportMailCountsDto>>;

/// <summary>
/// Number of the current user's mails with a staff reply they haven't read yet (mobile mail icon badge).
/// </summary>
public record GetMyUnreadSupportMailsCountQuery : IRequest<Result<int>>;

public class GetSupportMailCountsQueryHandler : IRequestHandler<GetSupportMailCountsQuery, Result<SupportMailCountsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetSupportMailCountsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<SupportMailCountsDto>> Handle(GetSupportMailCountsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id) || !Guid.TryParse(_user.Id, out var userId))
        {
            return Result<SupportMailCountsDto>.Failure(new[] { "Unauthorized" });
        }

        var mails = _context.SupportMails.AsNoTracking();
        var scope = request.AssignedToMe ? mails.Where(m => m.AssignedModeratorId == userId) : mails;

        var counts = new SupportMailCountsDto
        {
            AwaitingResponse = await scope.CountAsync(m => m.Status == SupportMailStatus.AwaitingResponse, cancellationToken),
            Unassigned = request.AssignedToMe
                ? 0
                : await mails.CountAsync(m => m.Status != SupportMailStatus.Closed && m.AssignedModeratorId == null, cancellationToken),
            AssignedToMeAwaiting = await mails.CountAsync(
                m => m.AssignedModeratorId == userId && m.Status == SupportMailStatus.AwaitingResponse, cancellationToken)
        };

        return Result<SupportMailCountsDto>.Success(counts);
    }
}

public class GetMyUnreadSupportMailsCountQueryHandler : IRequestHandler<GetMyUnreadSupportMailsCountQuery, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMyUnreadSupportMailsCountQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<int>> Handle(GetMyUnreadSupportMailsCountQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id) || !Guid.TryParse(_user.Id, out var userId))
        {
            return Result<int>.Failure(new[] { "Unauthorized" });
        }

        var count = await _context.SupportMails.CountAsync(m =>
            m.RequesterId == userId &&
            m.LastMessageSenderType == SupportMailSenderType.Staff &&
            (m.RequesterLastReadAt == null || m.RequesterLastReadAt < m.LastMessageAt), cancellationToken);

        return Result<int>.Success(count);
    }
}
