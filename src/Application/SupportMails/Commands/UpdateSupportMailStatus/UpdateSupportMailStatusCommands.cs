using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Commands.UpdateSupportMailStatus;

public record CloseSupportMailCommand(Guid Id) : IRequest<Result>;

public record ReopenSupportMailCommand(Guid Id) : IRequest<Result>;

public class CloseSupportMailCommandHandler : IRequestHandler<CloseSupportMailCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public CloseSupportMailCommandHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(CloseSupportMailCommand request, CancellationToken cancellationToken)
    {
        var mail = await _context.SupportMails.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (mail == null) return Result.Failure(new[] { "Mail not found" });

        if (mail.Status != SupportMailStatus.Closed)
        {
            mail.Status = SupportMailStatus.Closed;
            mail.ClosedAt = _timeProvider.GetUtcNow();
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

public class ReopenSupportMailCommandHandler : IRequestHandler<ReopenSupportMailCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public ReopenSupportMailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(ReopenSupportMailCommand request, CancellationToken cancellationToken)
    {
        var mail = await _context.SupportMails.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (mail == null) return Result.Failure(new[] { "Mail not found" });

        if (mail.Status == SupportMailStatus.Closed)
        {
            mail.Status = mail.LastMessageSenderType == SupportMailSenderType.Staff
                ? SupportMailStatus.Responded
                : SupportMailStatus.AwaitingResponse;
            mail.ClosedAt = null;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
