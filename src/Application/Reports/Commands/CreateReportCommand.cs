using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Reports.Commands;

public record CreateReportCommand : IRequest<Result<Guid>>
{
    public ReportType ReportType { get; init; }
    public Guid? ReportedContentId { get; init; }
    public ReportReason Reason { get; init; }
    public string Description { get; init; } = string.Empty;
}

public class CreateReportCommandHandler : IRequestHandler<CreateReportCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ILocalizationService _localizer;

    public CreateReportCommandHandler(IApplicationDbContext context, IUser currentUser, ILocalizationService localizer)
    {
        _context = context;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<Guid>.Failure(new[] { _localizer["Auth.NotAuthenticated"] });
        }

        // Check if user already reported this content
        var existingReport = await _context.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.ReportedContentId == request.ReportedContentId &&
                r.ReportType == request.ReportType &&
                r.ReportedByUserId == userId &&
                r.Status == ReportStatus.Pending,
                cancellationToken);

        if (existingReport != null)
        {
            return Result<Guid>.Failure(new[] { _localizer["Reports.AlreadyReported"] });
        }

        var report = new Report
        {
            ReportType = request.ReportType,
            ReportedContentId = request.ReportedContentId ?? Guid.Empty,
            Reason = request.Reason,
            Description = request.Description,
            Status = ReportStatus.Pending,
            ReportedByUserId = userId
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(report.Id);
    }
}

public class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.ReportType)
            .IsInEnum()
            .WithMessage("Invalid report type.");

        RuleFor(x => x.Reason)
            .IsInEnum()
            .WithMessage("Invalid report reason.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters.");
    }
}
