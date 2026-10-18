using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailById;

/// <summary>
/// Staff view of a mail thread. Allowed for the assigned moderator or users with ViewInquiryRequests.
/// </summary>
public record GetSupportMailByIdQuery(Guid Id) : IRequest<Result<SupportMailDetailDto>>;

public class GetSupportMailByIdQueryHandler : IRequestHandler<GetSupportMailByIdQuery, Result<SupportMailDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IPermissionService _permissionService;

    public GetSupportMailByIdQueryHandler(IApplicationDbContext context, IUser user, IPermissionService permissionService)
    {
        _context = context;
        _user = user;
        _permissionService = permissionService;
    }

    public async Task<Result<SupportMailDetailDto>> Handle(GetSupportMailByIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<SupportMailDetailDto>.Failure(new[] { "Unauthorized" });
        }

        var mail = await _context.SupportMails
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (mail == null ||
            !await SupportMailRules.CanStaffAccessAsync(mail, Guid.Parse(_user.Id), Permission.ViewInquiryRequests, _permissionService, cancellationToken))
        {
            return Result<SupportMailDetailDto>.Failure(new[] { "Mail not found" });
        }

        var dto = await _context.SupportMails
            .AsNoTracking()
            .Where(m => m.Id == request.Id)
            .Select(m => new SupportMailDetailDto
            {
                Id = m.Id,
                Number = m.Number,
                RequesterId = m.RequesterId,
                RequesterName = m.Requester != null ? m.Requester.FirstName + " " + m.Requester.LastName : string.Empty,
                RequesterEmail = m.Requester != null ? m.Requester.Email : null,
                AssignedModeratorId = m.AssignedModeratorId,
                AssignedModeratorName = m.AssignedModerator != null ? m.AssignedModerator.FirstName + " " + m.AssignedModerator.LastName : null,
                Topic = m.Topic,
                Subject = m.Subject,
                Status = m.Status,
                Created = m.Created,
                LastMessageAt = m.LastMessageAt,
                ClosedAt = m.ClosedAt,
                Messages = m.Messages
                    .OrderBy(msg => msg.Created)
                    .Select(msg => new SupportMailMessageDto
                    {
                        Id = msg.Id,
                        SenderType = msg.SenderType,
                        SenderName = msg.Sender != null ? msg.Sender.FirstName + " " + msg.Sender.LastName : null,
                        SenderProfilePictureUrl = msg.Sender != null ? msg.Sender.ImageUrl : null,
                        Body = msg.Body,
                        Created = msg.Created
                    })
                    .ToList()
            })
            .FirstAsync(cancellationToken);

        return Result<SupportMailDetailDto>.Success(dto);
    }
}
