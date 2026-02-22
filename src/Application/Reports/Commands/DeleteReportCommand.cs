using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Reports.Commands;

public record DeleteReportCommand(Guid Id) : IRequest<Result<Unit>>;

public class DeleteReportCommandHandler : IRequestHandler<DeleteReportCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public DeleteReportCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(DeleteReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report == null)
        {
            return Result<Unit>.Failure(new[] { "Report not found." });
        }

        _context.Reports.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
