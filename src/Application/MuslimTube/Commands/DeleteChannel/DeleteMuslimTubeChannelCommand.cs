using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.MuslimTube.Commands.DeleteChannel;

public record DeleteMuslimTubeChannelCommand(Guid ChannelId) : IRequest<bool>;

public class DeleteMuslimTubeChannelCommandHandler : IRequestHandler<DeleteMuslimTubeChannelCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteMuslimTubeChannelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteMuslimTubeChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.MuslimTubeChannels.FindAsync(new object[] { request.ChannelId }, cancellationToken);
        if (channel == null || channel.IsDeleted) return false;
        channel.IsDeleted = true; // soft delete
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
