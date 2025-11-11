using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.QAs.Commands.DeleteQA;

public record DeleteQACommand(Guid Id) : IRequest;

public class DeleteQACommandHandler : IRequestHandler<DeleteQACommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteQACommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteQACommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.QAs.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.QAs.Remove(entity!);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
