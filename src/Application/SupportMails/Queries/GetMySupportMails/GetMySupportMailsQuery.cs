using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Queries.GetMySupportMails;

public record GetMySupportMailsQuery : IRequest<Result<PaginatedList<MySupportMailListItemDto>>>, IPaginationQuery
{
    public SupportMailStatus? Status { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetMySupportMailsQueryHandler : IRequestHandler<GetMySupportMailsQuery, Result<PaginatedList<MySupportMailListItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMySupportMailsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<MySupportMailListItemDto>>> Handle(GetMySupportMailsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<PaginatedList<MySupportMailListItemDto>>.Failure(new[] { "Unauthorized" });
        }

        var requesterId = Guid.Parse(_user.Id);

        var query = _context.SupportMails
            .AsNoTracking()
            .Where(m => m.RequesterId == requesterId);

        if (request.Status.HasValue)
        {
            query = query.Where(m => m.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(m => m.Subject.ToLower().Contains(searchLower));
        }

        var paginatedList = await query
            .OrderByDescending(m => m.LastMessageAt)
            .Select(m => new MySupportMailListItemDto
            {
                Id = m.Id,
                Number = m.Number,
                Topic = m.Topic,
                Subject = m.Subject,
                Preview = m.Messages
                    .OrderBy(msg => msg.Created)
                    .Select(msg => msg.Body.Length > SupportMailRules.PreviewLength
                        ? msg.Body.Substring(0, SupportMailRules.PreviewLength)
                        : msg.Body)
                    .FirstOrDefault() ?? string.Empty,
                Status = m.Status,
                HasUnreadReply = m.LastMessageSenderType == SupportMailSenderType.Staff
                    && (m.RequesterLastReadAt == null || m.RequesterLastReadAt < m.LastMessageAt),
                Created = m.Created,
                LastMessageAt = m.LastMessageAt
            })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<MySupportMailListItemDto>>.Success(paginatedList);
    }
}

public class GetMySupportMailsQueryValidator : PaginationQueryValidator<GetMySupportMailsQuery>
{
    public GetMySupportMailsQueryValidator() : base()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }
}
