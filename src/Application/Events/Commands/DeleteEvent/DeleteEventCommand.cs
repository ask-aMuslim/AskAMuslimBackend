using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Events.Commands.DeleteEvent;

public record DeleteEventCommand(Guid Id) : IRequest;

public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Events.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
        if (entity != null)
        {
            _context.Events.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
