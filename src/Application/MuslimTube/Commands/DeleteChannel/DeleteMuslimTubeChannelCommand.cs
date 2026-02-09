using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.MuslimTube.Commands.DeleteChannel;

public record DeleteMuslimTubeChannelCommand([property: JsonIgnore] Guid ChannelId) : IRequest<Result<bool>>;

public class DeleteMuslimTubeChannelCommandHandler : IRequestHandler<DeleteMuslimTubeChannelCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteMuslimTubeChannelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(DeleteMuslimTubeChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.MuslimTubeChannels.FindAsync(new object[] { request.ChannelId }, cancellationToken);
        if (channel == null || channel.IsDeleted) 
            return Result<bool>.Success(false);
        
        channel.IsDeleted = true; // soft delete
        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
