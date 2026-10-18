using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMails;

/// <summary>
/// Staff listing. The admin endpoint is permission-gated; the moderator endpoint forces <see cref="AssignedToMe"/>.
/// </summary>
public record GetSupportMailsQuery : IRequest<Result<PaginatedList<SupportMailListItemDto>>>, IPaginationQuery
{
    public SupportMailStatus? Status { get; init; }
    public MeetingInquiryTopic? Topic { get; init; }
    public bool? IsAssigned { get; init; }
    public Guid? ModeratorId { get; init; }
    public bool AssignedToMe { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetSupportMailsQueryHandler : IRequestHandler<GetSupportMailsQuery, Result<PaginatedList<SupportMailListItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetSupportMailsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<SupportMailListItemDto>>> Handle(GetSupportMailsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SupportMails.AsNoTracking();

        if (request.AssignedToMe)
        {
            if (string.IsNullOrEmpty(_user.Id))
            {
                return Result<PaginatedList<SupportMailListItemDto>>.Failure(new[] { "Unauthorized" });
            }

            var currentUserId = Guid.Parse(_user.Id);
            query = query.Where(m => m.AssignedModeratorId == currentUserId);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }

        if (request.Topic.HasValue)
        {
            query = query.Where(m => m.Topic == request.Topic.Value);
        }

        if (request.IsAssigned.HasValue)
        {
            query = request.IsAssigned.Value
                ? query.Where(m => m.AssignedModeratorId.HasValue)
                : query.Where(m => !m.AssignedModeratorId.HasValue);
        }

        if (request.ModeratorId.HasValue)
        {
            query = query.Where(m => m.AssignedModeratorId == request.ModeratorId.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(m => m.Created >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(m => m.Created <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(m =>
                m.Subject.ToLower().Contains(searchLower) ||
                m.Messages.Any(msg => msg.Body.ToLower().Contains(searchLower)) ||
                (m.Requester != null && (m.Requester.FirstName + " " + m.Requester.LastName).ToLower().Contains(searchLower)) ||
                (m.Requester != null && m.Requester.Email != null && m.Requester.Email.ToLower().Contains(searchLower)));
        }

        var paginatedList = await query
            .OrderByDescending(m => m.LastMessageAt)
            .Select(m => new SupportMailListItemDto
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
                Preview = m.Messages
                    .OrderBy(msg => msg.Created)
                    .Select(msg => msg.Body.Length > SupportMailRules.PreviewLength
                        ? msg.Body.Substring(0, SupportMailRules.PreviewLength)
                        : msg.Body)
                    .FirstOrDefault() ?? string.Empty,
                Status = m.Status,
                MessagesCount = m.Messages.Count,
                Created = m.Created,
                LastMessageAt = m.LastMessageAt
            })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<SupportMailListItemDto>>.Success(paginatedList);
    }
}

public class GetSupportMailsQueryValidator : PaginationQueryValidator<GetSupportMailsQuery>
{
    public GetSupportMailsQueryValidator() : base()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Topic)
            .IsInEnum()
            .When(x => x.Topic.HasValue);
    }
}
