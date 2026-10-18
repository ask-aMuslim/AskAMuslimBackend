using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.SupportMails.Commands.UnassignSupportMail;

public record UnassignSupportMailCommand(Guid Id) : IRequest<Result>;

public class UnassignSupportMailCommandHandler : IRequestHandler<UnassignSupportMailCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UnassignSupportMailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UnassignSupportMailCommand request, CancellationToken cancellationToken)
    {
        var mail = await _context.SupportMails
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (mail == null)
        {
            return Result.Failure(new[] { "Mail not found" });
        }

        mail.AssignedModeratorId = null;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
