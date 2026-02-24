using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Reports.Queries;

public record GetAllReportsQuery : IRequest<Result<PaginatedList<ReportDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public ReportStatus? Status { get; init; }
    public ReportType? ReportType { get; init; }
    public ReportReason? Reason { get; init; }
    public Guid? ReportedByUserId { get; init; }
}

public class GetAllReportsQueryHandler : IRequestHandler<GetAllReportsQuery, Result<PaginatedList<ReportDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetAllReportsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<ReportDto>>> Handle(GetAllReportsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Reports
            .Include(r => r.ReportedBy)
            .Include(r => r.ReviewedBy)
            .AsNoTracking();

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (request.ReportType.HasValue)
        {
            query = query.Where(r => r.ReportType == request.ReportType.Value);
        }

        if (request.Reason.HasValue)
        {
            query = query.Where(r => r.Reason == request.Reason.Value);
        }

        if (request.ReportedByUserId.HasValue)
        {
            query = query.Where(r => r.ReportedByUserId == request.ReportedByUserId.Value);
        }

        var projected = query
            .OrderByDescending(r => r.Created)
            .Select(r => new ReportDto
            {
                Id = r.Id,
                ReportType = r.ReportType,
                ReportedContentId = r.ReportedContentId ?? Guid.Empty,
                Reason = r.Reason,
                Description = r.Description,
                Status = r.Status,
                ReportedByUserId = r.ReportedByUserId,
                ReportedByUserName = r.ReportedBy != null ? r.ReportedBy.FirstName + " " + r.ReportedBy.LastName : null,
                ReportedByUserEmail = r.ReportedBy != null ? r.ReportedBy.Email : null,
                ReviewedByUserId = r.ReviewedByUserId,
                ReviewedByUserName = r.ReviewedBy != null ? r.ReviewedBy.FirstName + " " + r.ReviewedBy.LastName : null,
                ReviewNotes = r.ReviewNotes,
                ReviewedAt = r.ReviewedAt,
                Created = r.Created,
                LastModified = r.LastModified
            });

        var paginatedList = await PaginatedList<ReportDto>.CreateAsync(
            projected,
            request.PageNumber ?? 1,
            request.PageSize ?? 10,
            cancellationToken);

        return Result<PaginatedList<ReportDto>>.Success(paginatedList);
    }
}

public class GetAllReportsQueryValidator : PaginationQueryValidator<GetAllReportsQuery>
{
    public GetAllReportsQueryValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.ReportType)
            .IsInEnum()
            .When(x => x.ReportType.HasValue);

        RuleFor(x => x.Reason)
            .IsInEnum()
            .When(x => x.Reason.HasValue);
    }
}
