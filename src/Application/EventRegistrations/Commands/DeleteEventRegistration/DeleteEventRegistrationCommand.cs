using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.EventRegistrations.Commands.DeleteEventRegistration;

public record DeleteEventRegistrationCommand(Guid Id) : IRequest<Result>;

public class DeleteEventRegistrationCommandHandler : IRequestHandler<DeleteEventRegistrationCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteEventRegistrationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EventRegistrations.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { $"Event registration with id {request.Id} not found" });
        }
        
        _context.EventRegistrations.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
