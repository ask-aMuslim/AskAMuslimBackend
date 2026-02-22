using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Reports.Queries;

public record GetReportByIdQuery(Guid Id) : IRequest<Result<ReportDto>>;

public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, Result<ReportDto>>
{
    private readonly IApplicationDbContext _context;

    public GetReportByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ReportDto>> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
    {
        var report = await _context.Reports
            .Include(r => r.ReportedBy)
            .Include(r => r.ReviewedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report == null)
        {
            return Result<ReportDto>.Failure(new[] { "Report not found." });
        }

        var dto = new ReportDto
        {
            Id = report.Id,
            ReportType = report.ReportType,
            ReportedContentId = report.ReportedContentId,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status,
            ReportedByUserId = report.ReportedByUserId,
            ReportedByUserName = report.ReportedBy != null ? $"{report.ReportedBy.FirstName} {report.ReportedBy.LastName}" : null,
            ReportedByUserEmail = report.ReportedBy?.Email,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedByUserName = report.ReviewedBy != null ? $"{report.ReviewedBy.FirstName} {report.ReviewedBy.LastName}" : null,
            ReviewNotes = report.ReviewNotes,
            ReviewedAt = report.ReviewedAt,
            Created = report.Created,
            LastModified = report.LastModified
        };

        return Result<ReportDto>.Success(dto);
    }
}
