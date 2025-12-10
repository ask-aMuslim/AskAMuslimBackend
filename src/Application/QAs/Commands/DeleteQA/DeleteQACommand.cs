using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.QAs.Commands.DeleteQA;

public record DeleteQACommand(Guid Id) : IRequest<Result>;

public class DeleteQACommandHandler : IRequestHandler<DeleteQACommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteQACommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteQACommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.QAs.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { "QA not found." });

        _context.QAs.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
