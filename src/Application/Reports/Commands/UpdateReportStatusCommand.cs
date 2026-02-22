using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Reports.Commands;

public record UpdateReportStatusCommand : IRequest<Result<Unit>>
{
    public Guid Id { get; init; }
    public ReportStatus Status { get; init; }
    public string? ReviewNotes { get; init; }
}

public class UpdateReportStatusCommandHandler : IRequestHandler<UpdateReportStatusCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateReportStatusCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Unit>> Handle(UpdateReportStatusCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report == null)
        {
            return Result<Unit>.Failure(new[] { "Report not found." });
        }

        if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            return Result<Unit>.Failure(new[] { "User not authenticated." });
        }

        report.Status = request.Status;
        report.ReviewedByUserId = userId;
        report.ReviewedAt = DateTime.UtcNow;
        
        if (!string.IsNullOrWhiteSpace(request.ReviewNotes))
        {
            report.ReviewNotes = request.ReviewNotes;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}

public class UpdateReportStatusCommandValidator : AbstractValidator<UpdateReportStatusCommand>
{
    public UpdateReportStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Report ID is required.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid status.");

        RuleFor(x => x.ReviewNotes)
            .MaximumLength(2000)
            .WithMessage("Review notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReviewNotes));
    }
}
