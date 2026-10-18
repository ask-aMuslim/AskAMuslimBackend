using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.SupportMails.Commands.MarkMySupportMailAsRead;

public record MarkMySupportMailAsReadCommand(Guid Id) : IRequest<Result>;

public class MarkMySupportMailAsReadCommandHandler : IRequestHandler<MarkMySupportMailAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public MarkMySupportMailAsReadCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(MarkMySupportMailAsReadCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result.Failure(new[] { "Unauthorized" });
        }

        var requesterId = Guid.Parse(_user.Id);

        var mail = await _context.SupportMails
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.RequesterId == requesterId, cancellationToken);

        if (mail == null)
        {
            return Result.Failure(new[] { "Mail not found" });
        }

        mail.RequesterLastReadAt = _timeProvider.GetUtcNow();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
