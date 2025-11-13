using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.DeleteEventRegistration;

public record DeleteEventRegistrationCommand(Guid Id) : IRequest;

public class DeleteEventRegistrationCommandHandler : IRequestHandler<DeleteEventRegistrationCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteEventRegistrationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity != null)
        {
            _context.EventRegistrations.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
